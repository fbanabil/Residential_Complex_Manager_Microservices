extern alias AuthApi;
using AuthApi::AuthenticationService.API.Apis.User.LocalLogin;
using AuthApi::AuthenticationService.API.AuthenticationDbContest;
using AuthApi::AuthenticationService.API.EntityModels;
using AuthApi::AuthenticationService.API.Enum;
using AuthApi::AuthenticationService.API.Helpers.Authenticate;
using AuthApi::AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using AuthApi::AuthenticationService.API.Helpers.VerificationToken;
using Microsoft.Extensions.Logging.Abstractions;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class LocalLoginHandlerTests
    {
        private static (LocalLoginHandler handler, AuthDbContext ctx, SqliteAuthDbContextScope scope,
                Mock<IAuthenticationTokenCreator> tokens, Mock<IPasswordHasher> hasher) Build()
        {
            var scope = new SqliteAuthDbContextScope();
            var tokens = new Mock<IAuthenticationTokenCreator>();
            var hasher = new Mock<IPasswordHasher>();
            var verif = new Mock<IVerificationTokenGenerator>();
            tokens.Setup(t => t.CreateToken(It.IsAny<UserPayload>())).ReturnsAsync("ACCESS.TOKEN.JWT");
            return (new LocalLoginHandler(scope.Context, tokens.Object, verif.Object,
                hasher.Object, NullLogger<LocalLoginHandler>.Instance), scope.Context, scope, tokens, hasher);
        }

        private static User SeedUser(AuthDbContext ctx, string email, bool emailVerified, string passwordHash)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = "User", Description = "U", CreatedAt = DateTime.UtcNow };
            ctx.Roles.Add(role);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Username = "u_" + Guid.NewGuid().ToString("N")[..6],
                Phone = "+8801712345678",
                PasswordHash = passwordHash,
                Status = Status.Active,
                IsEmailVerified = emailVerified,
                IsUserVerified = false,
                AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            ctx.Users.Add(user);
            ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
            ctx.SaveChanges();
            return user;
        }

        [Fact]
        public async Task Returns_USER_NOT_FOUND_when_email_unknown()
        {
            var (handler, _, scope, _, _) = Build();
            await using var _s = scope;

            var result = await handler.Handle(new LocalLoginCommand("ghost@example.com", "pw"), default);

            result.Result.Should().BeNull();
            result.Error!.Title.Should().Be("USER_NOT_FOUND");
            result.Error.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Returns_EMAIL_NOT_VERIFIED_when_user_email_not_verified()
        {
            var (handler, ctx, scope, _, _) = Build();
            await using var _s = scope;
            SeedUser(ctx, "u@example.com", emailVerified: false, passwordHash: "ANY");

            var result = await handler.Handle(new LocalLoginCommand("u@example.com", "pw"), default);

            result.Error!.Title.Should().Be("EMAIL_NOT_VERIFIED");
            result.Error.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task Returns_INVALID_PASSWORD_when_password_check_fails()
        {
            var (handler, ctx, scope, _, hasher) = Build();
            await using var _s = scope;
            SeedUser(ctx, "u@example.com", emailVerified: true, passwordHash: "HASH");
            hasher.Setup(h => h.VerifyPassword("pw", "HASH")).ReturnsAsync(false);

            var result = await handler.Handle(new LocalLoginCommand("u@example.com", "pw"), default);

            result.Error!.Title.Should().Be("INVALID_PASSWORD");
            result.Error.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task Returns_access_and_refresh_tokens_on_success_and_revokes_prior_active_refresh_tokens()
        {
            var (handler, ctx, scope, _, hasher) = Build();
            await using var _s = scope;
            var user = SeedUser(ctx, "u@example.com", emailVerified: true, passwordHash: "HASH");
            hasher.Setup(h => h.VerifyPassword("pw", "HASH")).ReturnsAsync(true);

            ctx.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "OLD_HASH",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                RevokedAt = null
            });
            ctx.SaveChanges();

            var result = await handler.Handle(new LocalLoginCommand("u@example.com", "pw"), default);

            result.Error.Should().BeNull();
            result.Result.Should().NotBeNull();
            result.Result!.AccessToken.Should().Be("ACCESS.TOKEN.JWT");
            result.Result.RefreshToken.Should().NotBeNullOrEmpty();

            ctx.RefreshTokens.Count(rt => rt.UserId == user.Id).Should().Be(2);
            ctx.RefreshTokens.Count(rt => rt.UserId == user.Id && rt.RevokedAt != null).Should().Be(1,
                "the previously-active refresh token must be revoked");
            ctx.RefreshTokens.Count(rt => rt.UserId == user.Id && rt.RevokedAt == null).Should().Be(1,
                "only the freshly-issued refresh token should remain active");
        }
    }
}
