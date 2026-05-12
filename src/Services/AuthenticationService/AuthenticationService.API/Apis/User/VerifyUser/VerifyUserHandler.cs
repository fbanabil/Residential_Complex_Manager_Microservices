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


        public VerifyUserHandler(AuthDbContext authDbContext)
        {
            _authDbContext = authDbContext;
        }


        public async Task<VerifyUserResult> Handle(VerifyUserCommand request, CancellationToken cancellationToken)
        {
            EntityModels.User? user = await _authDbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if(user == null)
            {
                return new VerifyUserResult(null, new ErrorCarrier()
                {
                    Title = "USER_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No user found with email: {request.Email}"
                });
            }

            if(user.IsUserVerified)
            {
                return new VerifyUserResult(new VerifyUserResponse(Success: true, Message: "User is already verified."), null);
            }

            try
            {
                await _authDbContext.Users.Where(u => u.Id == user.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsUserVerified, true), cancellationToken);
            }
            catch
            {
                return new VerifyUserResult(null, new ErrorCarrier()
                {
                    Title = "USER_VERIFICATION_FAILED",
                    StatusCode = 500,
                    Detail = $"Failed to verify user with email: {request.Email}."
                });
            }

            return new VerifyUserResult(new VerifyUserResponse(Success: true, Message: "User verified successfully."), null);
        }
    }
}
