using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.EntityModels;
using AuthenticationService.API.Enum;
using AuthenticationService.API.Helpers.Email;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.GetHostUrl;
using AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using AuthenticationService.API.Helpers.PasswordHelper.RandomPassword;
using AuthenticationService.API.Helpers.VerificationToken;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace AuthenticationService.API.Apis.User.ResetPassword
{
    public record ResetPasswordCommand(string Email) : ICommand<ResetPasswordResult>;
    public record ResetPasswordResult(ResetPasswordResponse? Result, ErrorCarrier? Error);

    public class ResetPasswordHandler : ICommandHandler<ResetPasswordCommand, ResetPasswordResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IVerificationTokenGenerator _verificationTokenGenerator;
        private readonly IGetHostUrl _getHostUrl;
        private readonly IEmailHelper _emailHelper;
        public ResetPasswordHandler(AuthDbContext authDbContext, IPasswordHasher passwordHasher, IVerificationTokenGenerator verificationTokenGenerator, IGetHostUrl getHostUrl, IEmailHelper emailHelper)
        {
            _authDbContext = authDbContext;
            _passwordHasher = passwordHasher;
            _verificationTokenGenerator = verificationTokenGenerator;
            _getHostUrl = getHostUrl;
            _emailHelper = emailHelper;
        }

        public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            EntityModels.User? user = await _authDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);


            if (user == null)
            {
                return new ResetPasswordResult(null, new ErrorCarrier()
                {
                    Title = "User Not Found",
                    StatusCode = 404,
                    Detail = $"No user found with email {request.Email}"
                });
            }



            string hostUrl = await _getHostUrl.GetHostUrlAsync();
            string token = await _verificationTokenGenerator.GenerateTokenAsync();
            string hashedToken = await _verificationTokenGenerator.HashTokenAsync(token);
            string verificationLink = $"{hostUrl}/user/reset-password/confirm?userId={user.Id}&token={token}";
            verificationLink = HtmlEncoder.Default.Encode(verificationLink);

            try
            {
                SecurityTokens passwordResetToken = new SecurityTokens
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = hashedToken,
                    Type = TokenType.PasswordReset,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false
                };

                await _authDbContext.SecurityTokens.AddAsync(passwordResetToken, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                return new ResetPasswordResult(null, new ErrorCarrier()
                {
                    Title = "Database Error",
                    StatusCode = 500,
                    Detail = "An error occurred while saving the password reset token."
                });
            }

            bool emailSent = await _emailHelper.SendEmail(user.Email, "Reset Password", $"Please click the link to reset your password : <a href=\"{verificationLink}\">Reset Password</a>.");

            if (!emailSent)
            {
                return new ResetPasswordResult(null, new ErrorCarrier()
                {
                    Title = "Email Error",
                    StatusCode = 500,
                    Detail = "An error occurred while sending the password reset email."
                });
            }

            return new ResetPasswordResult(new ResetPasswordResponse(true, "Password reset email sent successfully."), null);
        }
    }






    public record ResetPasswordConfirmCommand(Guid UserId, string Token) : ICommand<ResetPasswordConfirmResult>;
    public record ResetPasswordConfirmResult(string? NewPassword, ErrorCarrier? Error);

    public class ResetPasswordConfirmHandler : ICommandHandler<ResetPasswordConfirmCommand, ResetPasswordConfirmResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IVerificationTokenGenerator _verificationTokenGenerator;
        public ResetPasswordConfirmHandler(AuthDbContext authDbContext, IPasswordHasher passwordHasher, IVerificationTokenGenerator verificationTokenGenerator)
        {
            _authDbContext = authDbContext;
            _passwordHasher = passwordHasher;
            _verificationTokenGenerator = verificationTokenGenerator;
        }

        public async Task<ResetPasswordConfirmResult> Handle(ResetPasswordConfirmCommand request, CancellationToken cancellationToken)
        {
            // Validate the token
            SecurityTokens? tokenRecord = await _authDbContext.SecurityTokens.AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == request.UserId && t.Type == TokenType.PasswordReset && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow, cancellationToken);
            if (tokenRecord == null)
            {
                return new ResetPasswordConfirmResult(null, new ErrorCarrier()
                {
                    Title = "Invalid or Expired Token",
                    StatusCode = 400,
                    Detail = "The password reset token is invalid or has expired."
                });
            }


            bool isValidToken = await _verificationTokenGenerator.VerifyTokenAsync(request.Token, tokenRecord.Token);
            if (!isValidToken)
            {
                return new ResetPasswordConfirmResult(null, new ErrorCarrier()
                {
                    Title = "Invalid Token",
                    StatusCode = 400,
                    Detail = "The provided token is invalid."
                });
            }


            // Generate a new random password
            string newRandomPassword =  await RandomPasswordGenerator.Generate(12);
            string hashedNewPassword = await _passwordHasher.HashPassword(newRandomPassword);

            // Update the user's password and mark the token as used
            try
            {
                EntityModels.User? user = await _authDbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
                if (user == null)
                {
                    return new ResetPasswordConfirmResult(null, new ErrorCarrier()
                    {
                        Title = "User Not Found",
                        StatusCode = 404,
                        Detail = $"No user found with ID {request.UserId}"
                    });
                }


                // Update password and token status
                user.PasswordHash = hashedNewPassword;
                tokenRecord.IsUsed = true;
                _authDbContext.Users.Update(user);
                _authDbContext.SecurityTokens.Update(tokenRecord);
                await _authDbContext.SaveChangesAsync(cancellationToken);
                return new ResetPasswordConfirmResult(newRandomPassword, null);
            }
            catch 
            { 
                return new ResetPasswordConfirmResult(null, new ErrorCarrier()
                {
                    Title = "Database Error",
                    StatusCode = 500,
                    Detail = $"An error occurred while updating the user's password."
                });
            }
        }
    }
}