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
        private readonly ILogger<ChangePasswordHandler> _logger;

        public ChangePasswordHandler(AuthDbContext authDbContext, IPasswordHasher passwordHasher, ILogger<ChangePasswordHandler> logger)
        {
            _authDbContext = authDbContext;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            EntityModels.User? user = await _authDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.UserEmail, cancellationToken);

            // Validate new password and confirmation match
            if (user == null)
            {
                _logger.LogWarning("Change password failed: No user found with email {Email}", request.UserEmail);
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
                _logger.LogWarning("Change password failed: User with email {Email} has no password set", request.UserEmail);
                return new ChangePasswordResult(null, new ErrorCarrier()
                {
                    Title = "PASSWORD_NOT_SET",
                    StatusCode = 400,
                    Detail = $"User with email {request.UserEmail} does not have a password set."
                });
            }



            // Check if new password and confirmation match
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                _logger.LogWarning("Change password failed: new password and confirmation do not match for {Email}", request.UserEmail);
                return new ChangePasswordResult(null, new ErrorCarrier
                {
                    Title = "PASSWORD_MISMATCH",
                    StatusCode = 400,
                    Detail = "New password and confirmation password do not match."
                });
            }


            // Validate current password
            bool isCurrentPasswordValid = await _passwordHasher.VerifyPassword(request.CurrentPassword, user!.PasswordHash!);
            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("Change password failed: Invalid current password for user with email {Email}", request.UserEmail);
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
                _logger.LogError("Change password failed: Database error while updating password for user with email {Email}", request.UserEmail);
                return new ChangePasswordResult(null, new ErrorCarrier()
                {
                    Title = "PASSWORD_UPDATE_FAILED",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the password. Please try again later."
                });
            }

            _logger.LogInformation("Password changed successfully for user with email {Email}", request.UserEmail);
            return new ChangePasswordResult(new ChangePasswordResponse(true,"Password updated successfully."), null);
        }
    }
}
