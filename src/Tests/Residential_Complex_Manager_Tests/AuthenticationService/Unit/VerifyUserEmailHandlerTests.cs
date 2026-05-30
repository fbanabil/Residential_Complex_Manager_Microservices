extern alias AuthApi;
using AuthApi::AuthenticationService.API.Apis.User.VerifyUserEmail;
using AuthApi::AuthenticationService.API.AuthenticationDbContest;
using AuthApi::AuthenticationService.API.EntityModels;
using AuthApi::AuthenticationService.API.Enum;
using AuthApi::AuthenticationService.API.Helpers.VerificationToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class VerifyUserEmailHandlerTests
    {
        private static (VerifyUserEmailHandler handler, AuthDbContext ctx, SqliteAuthDbContextScope scope,
                Mock<IVerificationTokenGenerator> tokens) Build()
        {
            var scope = new SqliteAuthDbContextScope();
            var tokens = new Mock<IVerificationTokenGenerator>();
            return (new VerifyUserEmailHandler(scope.Context, tokens.Object,
                NullLogger<VerifyUserEmailHandler>.Instance), scope.Context, scope, tokens);
        }

        private static User SeedUser(AuthDbContext ctx, bool verified)
        {
            var u = new User
            {
                Id = Guid.NewGuid(),
                Email = $"u_{Guid.NewGuid():N}@example.com",
                Username = "u_" + Guid.NewGuid().ToString("N")[..6],
                PasswordHash = "h",
                Status = Status.Active,
                IsEmailVerified = verified,
                AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            ctx.Users.Add(u);
            ctx.SaveChanges();
            return u;
        }

        [Fact]
        public async Task Returns_ALREADY_VERIFIED_when_user_email_is_already_verified()
        {
            var (handler, ctx, scope, _) = Build();
            await using var _s = scope;
            var u = SeedUser(ctx, verified: true);

            var result = await handler.Handle(new VerifyUserEmailCommand(u.Id, "tok"), default);

            result.ErrorCarrier!.Title.Should().Be("ALREADY_VERIFIED");
        }

        [Fact]
        public async Task Returns_INVALID_REQUEST_when_no_tokens_exist_for_user()
        {
            var (handler, ctx, scope, _) = Build();
            await using var _s = scope;
            var u = SeedUser(ctx, verified: false);

            var result = await handler.Handle(new VerifyUserEmailCommand(u.Id, "tok"), default);

            result.ErrorCarrier!.Title.Should().Be("INVALID_REQUEST");
        }

        [Fact]
        public async Task Returns_EXPIRED_when_latest_token_is_past_expiry()
        {
            var (handler, ctx, scope, _) = Build();
            await using var _s = scope;
            var u = SeedUser(ctx, verified: false);
            ctx.SecurityTokens.Add(new SecurityTokens
            {
                Id = Guid.NewGuid(),
                UserId = u.Id,
                Token = "hashed",
                Type = TokenType.EmailConfirmation,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                IsUsed = false
            });
            ctx.SaveChanges();

            var result = await handler.Handle(new VerifyUserEmailCommand(u.Id, "raw"), default);

            result.ErrorCarrier!.Title.Should().Be("EXPIRED");
        }

        [Fact]
        public async Task Returns_INVALID_TOKEN_when_verification_fails()
        {
            var (handler, ctx, scope, tokens) = Build();
            await using var _s = scope;
            var u = SeedUser(ctx, verified: false);
            ctx.SecurityTokens.Add(new SecurityTokens
            {
                Id = Guid.NewGuid(),
                UserId = u.Id,
                Token = "hashed",
                Type = TokenType.EmailConfirmation,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            });
            ctx.SaveChanges();
            tokens.Setup(t => t.VerifyTokenAsync("raw", "hashed")).ReturnsAsync(false);

            var result = await handler.Handle(new VerifyUserEmailCommand(u.Id, "raw"), default);

            result.ErrorCarrier!.Title.Should().Be("INVALID_TOKEN");
        }

        [Fact]
        public async Task Marks_user_verified_and_token_used_on_success()
        {
            var (handler, ctx, scope, tokens) = Build();
            await using var _s = scope;
            var u = SeedUser(ctx, verified: false);
            var tokenId = Guid.NewGuid();
            ctx.SecurityTokens.Add(new SecurityTokens
            {
                Id = tokenId,
                UserId = u.Id,
                Token = "hashed",
                Type = TokenType.EmailConfirmation,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            });
            ctx.SaveChanges();
            tokens.Setup(t => t.VerifyTokenAsync("raw", "hashed")).ReturnsAsync(true);

            var result = await handler.Handle(new VerifyUserEmailCommand(u.Id, "raw"), default);

            result.ErrorCarrier.Should().BeNull();
            result.Result!.Success.Should().BeTrue();
            ctx.Users.AsNoTracking().Single(x => x.Id == u.Id).IsEmailVerified.Should().BeTrue();
            ctx.SecurityTokens.AsNoTracking().Single(x => x.Id == tokenId).IsUsed.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_preserves_security_tokens_belonging_to_other_users()
        {
            var (handler, ctx, scope, tokens) = Build();
            await using var _s = scope;
            var targetUser = SeedUser(ctx, verified: false);
            var otherUser  = SeedUser(ctx, verified: false);

            var targetToken = new SecurityTokens
            {
                Id = Guid.NewGuid(), UserId = targetUser.Id, Token = "h",
                Type = TokenType.EmailConfirmation,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5), IsUsed = false
            };
            var unrelatedToken = new SecurityTokens
            {
                Id = Guid.NewGuid(), UserId = otherUser.Id, Token = "h2",
                Type = TokenType.EmailConfirmation,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5), IsUsed = false
            };
            ctx.SecurityTokens.AddRange(targetToken, unrelatedToken);
            ctx.SaveChanges();
            tokens.Setup(t => t.VerifyTokenAsync("raw", "h")).ReturnsAsync(true);

            await handler.Handle(new VerifyUserEmailCommand(targetUser.Id, "raw"), default);

            ctx.SecurityTokens.AsNoTracking().Any(t => t.Id == unrelatedToken.Id).Should().BeTrue();
        }
    }
}
