using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.EntityModels;
using AuthenticationService.API.Enum;
using AuthenticationService.API.Helpers.Email;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.PasswordHelper;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Windows.Input;

namespace AuthenticationService.API.Apis.AddNewUser
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
        public AddNewUserHandler(AuthDbContext authDbContext, IEmailHelper emailHelper, IPasswordHasher passwordHasher, IImageSaver imageSaver)
        {
            _authDbContext = authDbContext;
            _emailHelper = emailHelper;
            _passworHasher = passwordHasher;
            _imageSaver = imageSaver;
        }

        public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            User? user = await _authDbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.UserName || u.Phone == request.Phone, cancellationToken);

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

            if(request.Password != request.ConfirmPassword)
            {
                return new RegisterUserResult(null, new ErrorCarrier
                {
                    Title = "PASSWORD_MISMATCH",
                    StatusCode = 400,
                    Detail = "Password and Confirm Password do not match."
                });
            }

            User userToAdd = new User()
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


        }
    }
}
