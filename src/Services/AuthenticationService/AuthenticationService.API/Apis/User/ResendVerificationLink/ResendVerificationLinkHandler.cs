using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.EntityModels;
using AuthenticationService.API.Helpers.Email;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.GetHostUrl;
using AuthenticationService.API.Helpers.VerificationToken;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace AuthenticationService.API.Apis.User.GetVerificationLink
{
    public record ResendVerificationLinkCommand(string Email) : ICommand<ResendVerificationLinkResult>;

    public record ResendVerificationLinkResult(ResendVerificationLinkResponse? Result, ErrorCarrier? Error);


    public class ResendVerificationLinkHandler : ICommandHandler<ResendVerificationLinkCommand, ResendVerificationLinkResult>
    {
        private readonly IVerificationTokenGenerator _verificationTokenGenerator;
        private readonly AuthDbContext _authDbContext;
        private readonly IEmailHelper _emailHelper;
        private readonly IGetHostUrl _getHostUrl;
        private readonly ILogger<ResendVerificationLinkHandler> _logger;

        public ResendVerificationLinkHandler(IVerificationTokenGenerator verificationTokenGenerator, AuthDbContext authDbContext, IEmailHelper emailHelper, IGetHostUrl getHostUrl, ILogger<ResendVerificationLinkHandler> logger)
        {
            _verificationTokenGenerator = verificationTokenGenerator;
            _authDbContext = authDbContext;
            _emailHelper = emailHelper;
            _getHostUrl = getHostUrl;
            _logger = logger;
        }

        public async Task<ResendVerificationLinkResult> Handle(ResendVerificationLinkCommand request, CancellationToken cancellationToken)
        {
            // Existing User Check

            EntityModels.User? user = await _authDbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Resend verification link failed: No user found with email {Email}", request.Email);
                return new ResendVerificationLinkResult(null, new ErrorCarrier
                {
                    Title = "USER_NOT_FOUND",
                    StatusCode = 404,
                    Detail = "No user found with the provided email address."
                });
            }

            if (user.IsEmailVerified)
            {
                _logger.LogWarning("Resend verification link failed: Email already verified for user with email {Email}", request.Email);
                return new ResendVerificationLinkResult(null, new ErrorCarrier
                {
                    Title = "EMAIL_ALREADY_VERIFIED",
                    StatusCode = 400,
                    Detail = "The email address is already verified."
                });
            }


            // Check for existing valid token

            SecurityTokens? existingToken = await _authDbContext.SecurityTokens.FirstOrDefaultAsync(t => t.UserId == user.Id && t.Type == Enum.TokenType.EmailConfirmation && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (existingToken != null)
            {
                var expiresIn = existingToken.ExpiresAt - DateTime.UtcNow;
                _logger.LogWarning("Resend verification link: active token already exists for user with email {Email}, expires in {Minutes} minutes", request.Email, (int)expiresIn.TotalMinutes);
                return new ResendVerificationLinkResult(null, new ErrorCarrier
                {
                    Title = "VERIFICATION_LINK_ALREADY_SENT",
                    StatusCode = 400,
                    Detail = $"A verification link has already been sent to this email address. Please check your inbox or spam folder. Else you can request a new verification link after the current one expires in {expiresIn.TotalMinutes:F0} minutes."
                });
            }



            // Generate new token and send email

            string hostUrl = await _getHostUrl.GetHostUrlAsync();
            string token = await _verificationTokenGenerator.GenerateTokenAsync();
            string hashedToken = await _verificationTokenGenerator.HashTokenAsync(token);
            string verificationLink = $"{hostUrl}/auth/verify-email?userId={user.Id}&token={token}";
            verificationLink = HtmlEncoder.Default.Encode(verificationLink);

            SecurityTokens verificationToken = new SecurityTokens
            {
                Id = Guid.NewGuid(),
                Token = hashedToken,
                Type = Enum.TokenType.EmailConfirmation,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false,
                UserId = user.Id
            };


            try
            {
                _authDbContext.SecurityTokens.Add(verificationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Resend verification link failed: Database error while saving verification token for user with email {Email}", request.Email);
                return new ResendVerificationLinkResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while saving the verification token."
                });
            }

            bool emailSent = await _emailHelper.SendEmail(request.Email, "Email Verification", $"Please verify your email by clicking on the following link: <a href=\"{verificationLink}\">Verify Email</a>");

            if (!emailSent)
            {
                _logger.LogError("Resend verification link failed: Unable to send verification email to {Email}", request.Email);
                return new ResendVerificationLinkResult(null, new ErrorCarrier
                {
                    Title = "EMAIL_SENDING_FAILED",
                    StatusCode = 500,
                    Detail = "Failed to send the verification email. Please try again later."
                });
            }

            _logger.LogInformation("Verification email resent successfully to {Email}", request.Email);
            return new ResendVerificationLinkResult(new ResendVerificationLinkResponse(Success : true, Message : "Verification email sent successfully."), null);
        }
    }
}
