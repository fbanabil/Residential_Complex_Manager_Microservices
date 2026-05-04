
using AuthenticationService.API.Apis.User.AddNewUser;
using AuthenticationService.API.Apis.User.LocalLogin;
using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.EntityModels;
using AuthenticationService.API.Enum;
using AuthenticationService.API.Helpers.Authenticate;
using AuthenticationService.API.Helpers.Email;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using AuthenticationService.API.Helpers.PasswordHelper.RandomPassword;
using AuthenticationService.API.Helpers.RefreashTokenHelper;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace AuthenticationService.API.Apis.User.OAuthLogins
{
    public record OAuthLoginsCommand(string GoogleId, string Email, string? Name,string? PictureUrl) : IRequest<OAuthLoginResult>;
    public record OAuthLoginResult(OAuthLoginResponse? LoginResponse, ErrorCarrier? Error);

    public class OAuthLoginsHandler : IRequestHandler<OAuthLoginsCommand, OAuthLoginResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IAuthenticationTokenCreator _authTokenCreator;
        private readonly IPasswordHasher _passworHasher;
        private readonly IEmailHelper _emailHelper;

        public OAuthLoginsHandler(AuthDbContext authDbContext, IAuthenticationTokenCreator authTokenCreator, IPasswordHasher passwordHasher, IEmailHelper emailHelper)
        {
            _authDbContext = authDbContext;
            _authTokenCreator = authTokenCreator;
            _passworHasher = passwordHasher;
            _emailHelper = emailHelper;
        }
        public async Task<OAuthLoginResult> Handle(OAuthLoginsCommand request, CancellationToken cancellationToken)
        {

            EntityModels.User? user = await _authDbContext.Users.AsNoTracking().Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);



            string msg = "";

            #region Validation and Existing User Check

            // Check if a user with the provided email already exists in the database
            if (user is not null)
            {
                if (user.IsEmailVerified is false)
                {
                    try
                    {
                        await _authDbContext.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(u => u.SetProperty(u => u.IsEmailVerified, true), cancellationToken);
                    }
                    catch
                    {
                        return new OAuthLoginResult(null, new ErrorCarrier()
                        {
                            Title = "INTERNAL_SERVER_ERROR",
                            StatusCode = 500,
                            Detail = "An error occurred while updating the user email verification status."
                        });
                    }
                    msg = "Your email got verified";
                }

                // Create JWT token for the existing user
                UserPayload userPayload = new UserPayload(
                    UserId: user.Id.ToString(),
                    Email: user.Email,
                    Username: user.Username,
                    Roles: user.UserRoles.Select(ur => ur.Role!.Name).ToList());

                // Generate access token and refresh token for the existing user
                string accessToken = await _authTokenCreator.CreateToken(userPayload);
                string refreshToken = await RefreashTokenGenerator.CreateTokenAsync();

                return new OAuthLoginResult(new OAuthLoginResponse(accessToken, refreshToken, msg), null);

            }
            #endregion



            #region New User Creation and Role Assignment

            EntityModels.User newUser = new EntityModels.User()
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Username = request.Email,
                Phone = null,
                PasswordHash = null,
                Status = Status.Active,
                IsEmailVerified = true,
                IsUserVerified = false,
                ProfileImageId = null,
                NidImage = null,
                AuthProvider = AuthProvider.Google,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            /// Generate a random password for the new user and hash it before saving to the database
            string RandomPassword = await RandomPasswordGenerator.Generate(12);
            string hashedPassword = await _passworHasher.HashPassword(RandomPassword);

            newUser.PasswordHash = hashedPassword;


            msg = msg + "A new account has been created for you with Google OAuth. Your email is verified, and you can log in using Google. An email with your temporary password has been sent to your email address. You can reset your password after logging in.";



            // Save the new user to the database
            try
            {
                await _authDbContext.Users.AddAsync(newUser, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                return new OAuthLoginResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while creating a new user account. Please try again later."
                });
            }



            // Check if the default "User" role exists, if not create it
            EntityModels.Role? userRoleEntity = await _authDbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

            // If the "User" role does not exist, create it
            if (userRoleEntity == null)
            {
                try
                {
                    await _authDbContext.Roles.AddAsync(new EntityModels.Role
                    {
                        Id = Guid.NewGuid(),
                        Name = "User",
                        Description = "Default role for regular users",
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                    await _authDbContext.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    _authDbContext.Users.Remove(newUser);
                    await _authDbContext.SaveChangesAsync(cancellationToken);
                    return new OAuthLoginResult(null, new ErrorCarrier()
                    {
                        Title = "INTERNAL_SERVER_ERROR",
                        StatusCode = 500,
                        Detail = "An error occurred while creating the default user role. Please try again later."
                    });
                }
            }

            // Retrieve the "User" role again to get its ID
            userRoleEntity = await _authDbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);
            UserRole userRole = new UserRole
            {
                UserId = newUser.Id,
                RoleId = userRoleEntity!.Id,
                AssignedAt = DateTime.UtcNow
            };


            // Assign the "User" role to the new user
            try
            {
                await _authDbContext.UserRoles.AddAsync(userRole, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _authDbContext.Users.Remove(newUser);
                await _authDbContext.SaveChangesAsync(cancellationToken);
                return new OAuthLoginResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning the user role. Please try again later."
                });

            }

            #endregion



            // Send an email to the user with their temporary password and instructions to reset it after logging in
            bool passwordMail = await _emailHelper.SendEmail(newUser.Email,"Your Temporary Password for OAuth Login", $"A new account has been created for you with Google    OAuth. Your email is verified, and you can log in using Google. Your temporary password is: {RandomPassword}. Please reset your password after logging in.");

            if(!passwordMail)
            {
                msg = msg + " However, we were unable to send you an email with the temporary password. Please try to reset your password manually.";
            }



            // Create JWT token for the new user
            UserPayload newUserPayload = new UserPayload(
                UserId: newUser.Id.ToString(),
                Email: newUser.Email,
                Username: newUser.Username,
                Roles: new List<string> { "User" });
            // Generate access token and refresh token for the new user
            string newAccessToken = await _authTokenCreator.CreateToken(newUserPayload);
            string newRefreshToken = await RefreashTokenGenerator.CreateTokenAsync();

            try
            {
                await _authDbContext.RefreshTokens.Where(rt => rt.UserId == newUser.Id && rt.RevokedAt == null).ForEachAsync(rt =>
                {
                    rt.RevokedAt = DateTime.UtcNow;
                }, cancellationToken);
                await _authDbContext.RefreshTokens.AddAsync(new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = newUser.Id,
                    TokenHash = await RefreashTokenGenerator.HashTokenAsync(newRefreshToken),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                }, cancellationToken);

                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                msg = msg + " Something went wrong for refresh token. You may need to log in again after some time.";
            }



            return new OAuthLoginResult(new OAuthLoginResponse(newAccessToken, newRefreshToken, msg), null);
        }
    }
}



