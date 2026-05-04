using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;
using System.Windows.Input;

namespace AuthenticationService.API.Apis.User.ChangePassword
{
    public record ChangePasswordCommand(string CurrentPassword, string NewPassword, string ConfirmNewPassword, string UserEmail): ICommand<ChangePasswordResult>;

    public record ChangePasswordResult(ChangePasswordResponse? Result, ErrorCarrier? Error);

    public class ChangePasswordHandler : ICommandHandler<ChangePasswordCommand, ChangePasswordResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IPasswordHasher _passwordHasher;

        public ChangePasswordHandler(AuthDbContext authDbContext, IPasswordHasher passwordHasher)
        {
            _authDbContext = authDbContext;
            _passwordHasher = passwordHasher;
        }

        public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            EntityModels.User? user = await _authDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.UserEmail, cancellationToken);

            // Validate new password and confirmation match
            if (user == null)
            {
                return new ChangePasswordResult(null, new ErrorCarrier()
                        {
                            Title = "USER_NOT_FOUND",
                            StatusCode = 404,
                            Detail = $"No user found with email {request.UserEmail}"
                });
            }


            // Check if the user has a password set
            if (user.PasswordHash is null)
            {
                return new ChangePasswordResult(null, new ErrorCarrier()
                {
                    Title = "PASSWORD_NOT_SET",
                    StatusCode = 400,
                    Detail = $"User with email {request.UserEmail} does not have a password set."
                });
            }



            // Validate current password
            bool isCurrentPasswordValid = await _passwordHasher.VerifyPassword(request.CurrentPassword, user!.PasswordHash!);
            if (!isCurrentPasswordValid)
            {
                return new ChangePasswordResult(null, new ErrorCarrier()
                {
                    Title = "INVALID_CURRENT_PASSWORD",
                    StatusCode = 400,
                    Detail = "The current password provided is incorrect."
                });
            }



            // Hash the new password and update the user's password hash in the database
            string newHashedPassword = await _passwordHasher.HashPassword(request.NewPassword);
            try
            {
                await _authDbContext.Users.Where(u => u.Email == request.UserEmail).ExecuteUpdateAsync(u => u.SetProperty(user => user.PasswordHash, newHashedPassword), cancellationToken);
            }
            catch
            {
                return new ChangePasswordResult(null, new ErrorCarrier()
                {
                    Title = "PASSWORD_UPDATE_FAILED",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the password. Please try again later."
                });
            }

            

            return new ChangePasswordResult(new ChangePasswordResponse(true,"Password updated successfully."), null);
        }
    }
}
