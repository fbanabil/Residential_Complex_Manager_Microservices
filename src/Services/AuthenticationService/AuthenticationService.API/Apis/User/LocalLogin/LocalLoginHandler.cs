using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Helpers.Authenticate;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using AuthenticationService.API.Helpers.RefreashTokenHelper;
using AuthenticationService.API.Helpers.VerificationToken;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.Apis.User.LocalLogin
{
    public record LocalLoginCommand(string Email, string Password) : IRequest<LocalLoginResult>;
    public record LocalLoginResult(LocalLoginResponse? Result, ErrorCarrier? Error);
    public class LocalLoginHandler : IRequestHandler<LocalLoginCommand, LocalLoginResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly IAuthenticationTokenCreator _authenticationService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<LocalLoginHandler> _logger;

        public LocalLoginHandler(AuthDbContext authDbContext, IAuthenticationTokenCreator authenticationService, IVerificationTokenGenerator tokenGenerator, IPasswordHasher passwordHasher, ILogger<LocalLoginHandler> logger)
        {
            _authDbContext = authDbContext;
            _authenticationService = authenticationService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<LocalLoginResult> Handle(LocalLoginCommand request, CancellationToken cancellationToken)
        {
            // Check if verified user exist
            EntityModels.User? user = await _authDbContext.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: No user found with email {Email}", request.Email);
                return new LocalLoginResult(null, new ErrorCarrier()
                {
                    Title = "USER_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No user found with email {request.Email}"
                });
            }


            // Check if email is verified
            if (user.IsEmailVerified == false)
            {
                _logger.LogWarning("Login attempt failed: Email not verified for user with email {Email}", request.Email);
                return new LocalLoginResult(null, new ErrorCarrier()
                {
                    Title = "EMAIL_NOT_VERIFIED",
                    StatusCode = 403,
                    Detail = $"The email {request.Email} has not been verified. Please verify your email before logging in."
                });
            }


            // Verify password
            bool isPasswordValid = await _passwordHasher.VerifyPassword(request.Password, user.PasswordHash!);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login attempt failed: Invalid password for user with email {Email}", request.Email);
                return new LocalLoginResult(null, new ErrorCarrier()
                {
                    Title = "INVALID_PASSWORD",
                    StatusCode = 401,
                    Detail = "The provided password is incorrect."
                });
            }


            // Create JWT token
            var payload = new UserPayload(
                user.Id.ToString(),
                user.Username,
                user.Email,
                user.UserRoles.Select(ur => ur.Role!.Name).ToList()
            );

            string token =await _authenticationService.CreateToken(payload);


            // Create Refresh Token
            string refreshToken = await RefreashTokenGenerator.CreateTokenAsync();


            // Store the refresh token in the database
            EntityModels.RefreshToken newRefreshToken = new EntityModels.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ClientId = null, 
                TokenHash = await RefreashTokenGenerator.HashTokenAsync(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7), 
                CreatedAt = DateTime.UtcNow,
                RevokedAt = null,
                DeviceInfo = null
            };

            await using var transaction = await _authDbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _authDbContext.RefreshTokens.Where(rt => rt.UserId == user.Id && rt.RevokedAt == null).ForEachAsync(rt =>
                {
                    rt.RevokedAt = DateTime.UtcNow;
                }, cancellationToken);

                await _authDbContext.RefreshTokens.AddAsync(newRefreshToken, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to store refresh token for user with email {Email}. User is authenticated but failed to store refresh token.", request.Email);
                await transaction.RollbackAsync(cancellationToken);
                // User is authenticated but failed to store refresh token, so continue without refresh token.
            }


            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to commit transaction for storing refresh token for user with email {Email}. User is authenticated but failed to store refresh token.", request.Email); 
                await transaction.RollbackAsync(cancellationToken);
                // User is authenticated but failed to store refresh token, so continue without refresh token.
            }


            return new LocalLoginResult(new LocalLoginResponse(token, refreshToken), null);
        }
    }
}
