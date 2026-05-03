using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.EntityModels;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.VerificationToken;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticationService.API.Apis.User.VerifyUserEmail
{

    public record VerifyUserEmailCommand(Guid UserId, string VerificationToken) : ICommand<VerifyUserEmailResult>;
    public record VerifyUserEmailResult(VerifyUserEmailResponse? Result, ErrorCarrier? ErrorCarrier);

    public class VerifyUserEmailHandler : ICommandHandler<VerifyUserEmailCommand, VerifyUserEmailResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IVerificationTokenGenerator _verificationTokenGenerator;

        public VerifyUserEmailHandler(AuthDbContext authDbContext, IVerificationTokenGenerator verificationTokenGenerator)
        {
            _authDbContext = authDbContext;
            _verificationTokenGenerator = verificationTokenGenerator;
        }

        public async Task<VerifyUserEmailResult> Handle(VerifyUserEmailCommand request, CancellationToken cancellationToken)
        {

            // Check if the user exists and if their email is already verified

            EntityModels.User? user = await _authDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId);

            if(user is not null && user.IsEmailVerified is true)
            {
                return new VerifyUserEmailResult(null,
                    new ErrorCarrier()
                    {
                        Title = "ALREADY_VERIFIED",
                        StatusCode = 400,
                        Detail = "The user's email is already verified."
                    });
            }

            // Fetch all tokens from the database for the given user and token type

            var usersTokens = await _authDbContext.SecurityTokens.AsNoTracking().Where(t => t.UserId == request.UserId && t.Type == Enum.TokenType.EmailConfirmation).ToListAsync();

            // Check if the token exists

            if (usersTokens == null || !usersTokens.Any())
            {
                return new VerifyUserEmailResult(null,
                    new ErrorCarrier()
                    {
                        Title = "INVALID_REQUEST",
                        StatusCode = 400,
                        Detail = "No email verification token found for the user."
                    });
            }

            // Get latest token
            EntityModels.SecurityTokens actualToken = usersTokens.OrderByDescending(t => t.ExpiresAt).FirstOrDefault()!;

            await _authDbContext.SecurityTokens.Where(t => t.Id != actualToken.Id).ExecuteDeleteAsync(cancellationToken);



            // Check if the email is already verified

            if (actualToken.IsUsed)
            {
                return new VerifyUserEmailResult(null,
                    new ErrorCarrier()
                    {
                        Title = "ALREADY_USED",
                        StatusCode = 400,
                        Detail = "The email verification token has already been used."
                    });
            }



            // Check if the token has expired

            if (actualToken.ExpiresAt < DateTime.UtcNow)
            {
                return new VerifyUserEmailResult(null,
                    new ErrorCarrier()
                    {
                        Title = "EXPIRED",
                        StatusCode = 400,
                        Detail = "The email verification token has expired. Try resending the email verification link."
                    });
            }



            // Verify the token

            if (await _verificationTokenGenerator.VerifyTokenAsync(request.VerificationToken, actualToken.Token) is false)
            {
                return new VerifyUserEmailResult(null,
                    new ErrorCarrier()
                    {
                        Title = "INVALID_TOKEN",
                        StatusCode = 400,
                        Detail = "The email verification token is invalid."
                    });
            }



            // Mark the token as used and update the user's email verification status in a transaction

            try
            {
                await _authDbContext.SecurityTokens.Where(s => s.Id == actualToken.Id).ExecuteDeleteAsync(cancellationToken);
                await _authDbContext.Users.Where(u => u.Id == actualToken.UserId).ExecuteUpdateAsync(setter => setter.SetProperty(u => u.IsEmailVerified, true));
            }
            catch
            {
                return new VerifyUserEmailResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "Something went wront, please try again"
                });
            }


            return new VerifyUserEmailResult(new VerifyUserEmailResponse(Success : true,Message : "Email verified Successfully"), null);

        }
    }
}
