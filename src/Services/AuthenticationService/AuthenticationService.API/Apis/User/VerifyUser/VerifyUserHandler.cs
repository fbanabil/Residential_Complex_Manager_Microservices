using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Helpers.ErrorCarrier;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.Apis.User.VerifyUser
{
    public record VerifyUserCommand(string Email) : ICommand<VerifyUserResult>;

    public record VerifyUserResult(VerifyUserResponse? Result, ErrorCarrier? Error);

    public class VerifyUserHandler : ICommandHandler<VerifyUserCommand, VerifyUserResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly ILogger<VerifyUserHandler> _logger;


        public VerifyUserHandler(AuthDbContext authDbContext, ILogger<VerifyUserHandler> logger)
        {
            _authDbContext = authDbContext;
            _logger = logger;
        }


        public async Task<VerifyUserResult> Handle(VerifyUserCommand request, CancellationToken cancellationToken)
        {
            EntityModels.User? user = await _authDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if(user == null)
            {
                _logger.LogWarning("Verify user failed: No user found with email {Email}", request.Email);
                return new VerifyUserResult(null, new ErrorCarrier()
                {
                    Title = "USER_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No user found with email: {request.Email}"
                });
            }

            if(user.IsUserVerified)
            {
                _logger.LogInformation("Verify user: user with email {Email} is already verified", request.Email);
                return new VerifyUserResult(new VerifyUserResponse(Success: true, Message: "User is already verified."), null);
            }

            try
            {
                await _authDbContext.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsUserVerified, true), cancellationToken);
            }
            catch
            {
                _logger.LogError("Verify user failed: Database error while verifying user with email {Email}", request.Email);
                return new VerifyUserResult(null, new ErrorCarrier()
                {
                    Title = "USER_VERIFICATION_FAILED",
                    StatusCode = 500,
                    Detail = $"Failed to verify user with email: {request.Email}."
                });
            }

            _logger.LogInformation("User verified successfully with email {Email}", request.Email);
            return new VerifyUserResult(new VerifyUserResponse(Success: true, Message: "User verified successfully."), null);
        }
    }
}
