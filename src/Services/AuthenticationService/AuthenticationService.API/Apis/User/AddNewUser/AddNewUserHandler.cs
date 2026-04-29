using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.EntityModels;
using AuthenticationService.API.Enum;
using AuthenticationService.API.Helpers.Email;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.GetHostUrl;
using AuthenticationService.API.Helpers.NewFolder;
using AuthenticationService.API.Helpers.PasswordHelper;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Text.Encodings.Web;
using System.Windows.Input;

namespace AuthenticationService.API.Apis.User.AddNewUser
{
    public record RegisterUserCommand(
        string UserName,
        string Email,
        string Password,
        string ConfirmPassword,
        string Phone,
        string Bas64ProfileImage,
        string Base64NidImage
    ) : ICommand<RegisterUserResult>;

    public record RegisterUserResult(RegisterUserResponse? Response, ErrorCarrier? ErrorCarrier);


    public class AddNewUserHandler : ICommandHandler<RegisterUserCommand, RegisterUserResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IEmailHelper _emailHelper;
        private readonly IPasswordHasher _passworHasher;
        private readonly IImageSaver _imageSaver;
        private readonly IGetHostUrl _getHostUrl;
        private readonly IVerificationTokenGenerator _verificationTokenGenerator;

        public AddNewUserHandler(AuthDbContext authDbContext, IEmailHelper emailHelper, IPasswordHasher passwordHasher, IImageSaver imageSaver, IGetHostUrl getHostUrl, IVerificationTokenGenerator verificationTokenGenerator)
        {
            _authDbContext = authDbContext;
            _emailHelper = emailHelper;
            _passworHasher = passwordHasher;
            _imageSaver = imageSaver;
            _getHostUrl = getHostUrl;
            _verificationTokenGenerator = verificationTokenGenerator;
        }

        public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {

            #region Validations and Uniqueness Checks

            EntityModels.User? user = await _authDbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.UserName || u.Phone == request.Phone, cancellationToken);

            if (user != null)
            {
                return new RegisterUserResult(null, new ErrorCarrier
                {
                    Title = "USER_ALREADY_EXISTS",
                    StatusCode = 409,
                    Detail = user.Email == request.Email ? "A user with this email already exists." :
                             user.Username == request.UserName ? "A user with this username already exists." :
                                "A user with this phone number already exists."
                });
            }

            #endregion


            #region New User Creation and Role Assignment

            EntityModels.User userToAdd = new EntityModels.User()
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = request.UserName,
                Phone = request.Phone,
                PasswordHash = await _passworHasher.HashPassword(request.Password),
                Status = Status.Active,
                IsUserVerified = false,
                IsEmailVerified = false,
                AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = null
            };

            try
            {
                await _authDbContext.Users.AddAsync(userToAdd, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return new RegisterUserResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while saving the user to the database. Please try again later."
                });
            }


            EntityModels.Role? userRoleEntity = await _authDbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

            if (userRoleEntity == null)
            {
                _authDbContext.Users.Remove(userToAdd);
                await _authDbContext.SaveChangesAsync(cancellationToken);
                return new RegisterUserResult(null, new ErrorCarrier
                {
                    Title = "ROLE_NOT_FOUND",
                    StatusCode = 500,
                    Detail = "The default user role was not found in the database. Please contact support."
                });
            }


            UserRole userRole = new UserRole
            {
                UserId = userToAdd.Id,
                RoleId = userRoleEntity.Id,
                AssignedAt = DateTime.UtcNow
            };




            try
            {
                await _authDbContext.UserRoles.AddAsync(userRole, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _authDbContext.Users.Remove(userToAdd);
                await _authDbContext.SaveChangesAsync(cancellationToken);
                return new RegisterUserResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning the user role. Please try again later."
                });
            }

            #endregion


            #region Image Handling
            string? profileImageResult = null, nidImageResult = null, msg = string.Empty;

            if (!string.IsNullOrEmpty(request.Bas64ProfileImage))
            {
                try
                {
                    profileImageResult = await _imageSaver.SaveImageAsync(request.Bas64ProfileImage, "wwwroot/images/profileImages");
                }
                catch (Exception ex)
                {
                    msg= "Failed to save profile image, Update later.";
                    profileImageResult = "wwwroot/images/default.jpg";
                }

            }

            if (!string.IsNullOrEmpty(request.Base64NidImage))
            {
                try
                {
                    nidImageResult = await _imageSaver.SaveImageAsync(request.Base64NidImage, "wwwroot/images/nidImages");
                }
                catch (Exception ex)
                {
                    msg = msg + " Failed to save NID image, Update later.";
                    nidImageResult = "wwwroot/images/default.jpg";
                }
            }


            Image profileImage = new Image
            {
                Id = Guid.NewGuid(),
                UserId = userToAdd.Id,
                ImageType = ImageTypes.ProfileImage,
                ImagePath = profileImageResult!
            };

            Image nidImage = new Image
            {
                Id = Guid.NewGuid(),
                UserId = userToAdd.Id,
                ImageType = ImageTypes.NidImage,
                ImagePath = nidImageResult!
            };

            await _authDbContext.Images.AddRangeAsync(new[] { profileImage, nidImage }, cancellationToken);
            await _authDbContext.SaveChangesAsync(cancellationToken);

            userToAdd.ProfileImageId = profileImage.Id;
            userToAdd.NidImageId = nidImage.Id;

            await _authDbContext.SaveChangesAsync(cancellationToken);
            #endregion


            #region Email Verification and Token Generation

            string hostUrl =await _getHostUrl.GetHostUrlAsync();
            string token = await _verificationTokenGenerator.GenerateTokenAsync();
            string hashedToken = await _verificationTokenGenerator.HashTokenAsync(token);
            string verificationLink = $"{hostUrl}/auth/verify-email?userId={userToAdd.Id}&token={token}";
            verificationLink = HtmlEncoder.Default.Encode(verificationLink);

            try
            {
                SecurityTokens emailVerificationToken = new SecurityTokens
                {
                    Id = Guid.NewGuid(),
                    UserId = userToAdd.Id,
                    Token = hashedToken,
                    Type = TokenType.EmailConfirmation,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsUsed = false
                };

                await _authDbContext.SecurityTokens.AddAsync(emailVerificationToken, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                msg = msg + " Failed to generate email verification token, Please verify your email later.";
            }


            
             bool mail = await _emailHelper.SendEmail(userToAdd.Email, "Email Verification", $"Please verify your email by clicking on the following link: <a href=\"{verificationLink}\">Verify Email</a>");
            
            if(!mail)
            {
                msg = msg+ " Failed to send verification email, Please verify your email later.";
            }


            #endregion


            RegisterUserResponse response = new RegisterUserResponse(
                UserId: userToAdd.Id,
                UserName: userToAdd.Username,
                Email: userToAdd.Email,
                Message: "User registered successfully. Please verify your email to activate your account." + msg
            );

            return new RegisterUserResult(response, null);
        }
    }
}
