using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.EntityModels;
using AuthenticationService.API.Helpers.Authenticate;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.RefreashTokenHelper;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.Apis.User.RefreashToken
{
    public record RefreashTokenCommand(string Email, string RefreashToken) : ICommand<RefreashTokenResult>;

    public record RefreashTokenResult(RefreashTokenResponse? Result, ErrorCarrier? Error);

    public class RefreashTokenHandler : ICommandHandler<RefreashTokenCommand, RefreashTokenResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IAuthenticationTokenCreator _authenticationTokenCreator;

        public RefreashTokenHandler(AuthDbContext authDbContext, IAuthenticationTokenCreator authenticationTokenCreator)
        {
            _authDbContext = authDbContext;
            _authenticationTokenCreator = authenticationTokenCreator;
        }

        public async Task<RefreashTokenResult> Handle(RefreashTokenCommand request, CancellationToken cancellationToken)
        {
            // Validate the user exists
            EntityModels.User? userExist = await _authDbContext.Users.AsNoTracking().Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
            if (userExist == null)
            {
                return new RefreashTokenResult(null, new ErrorCarrier()
                {
                    Title = "USER_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No user found with email: {request.Email}"
                });
            }


            // Validate the refresh token exists and is valid
            EntityModels.RefreshToken? refreashToken = await _authDbContext.RefreshTokens.AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.UserId == userExist.Id && rt.RevokedAt==null && rt.ExpiresAt > DateTime.UtcNow, cancellationToken);
            if (refreashToken == null)
            {
                return new RefreashTokenResult(null, new ErrorCarrier()
                {
                    Title = "REFRESH_TOKEN_NOT_FOUND",
                    StatusCode = 403,
                    Detail = $"No valid refresh token found for user with email: {request.Email}"
                });
            }

            bool isVaidToken = await RefreashTokenGenerator.VerifyTokenAsync(request.RefreashToken, refreashToken.TokenHash);

            if (!isVaidToken)
            {
                return new RefreashTokenResult(null, new ErrorCarrier()
                {
                    Title = "INVALID_REFRESH_TOKEN",
                    StatusCode = 403,
                    Detail = $"The provided refresh token is invalid for user with email: {request.Email}"
                });
            }


            // Generate a new access token
            UserPayload userPayload = new UserPayload(UserId: userExist.Id.ToString(), Username: userExist.Username, Email: userExist.Email, Roles: userExist.UserRoles.Select(ur => ur.Role!.Name).ToList());
            string accessToken = await _authenticationTokenCreator.CreateToken(userPayload);


            return new RefreashTokenResult(new RefreashTokenResponse(accessToken), null);
        }
    }
}
