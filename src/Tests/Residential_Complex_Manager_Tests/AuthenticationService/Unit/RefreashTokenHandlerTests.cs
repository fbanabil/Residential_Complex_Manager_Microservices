extern alias AuthApi;
using AuthApi::AuthenticationService.API.Apis.User.RefreashToken;
using AuthApi::AuthenticationService.API.AuthenticationDbContest;
using AuthApi::AuthenticationService.API.EntityModels;
using AuthApi::AuthenticationService.API.Enum;
using AuthApi::AuthenticationService.API.Helpers.Authenticate;
using AuthApi::AuthenticationService.API.Helpers.RefreashTokenHelper;
using Microsoft.Extensions.Logging.Abstractions;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class RefreashTokenHandlerTests
    {
        private static (RefreashTokenHandler handler, AuthDbContext ctx, SqliteAuthDbContextScope scope,
                Mock<IAuthenticationTokenCreator> tokens) Build()
        {
            var scope = new SqliteAuthDbContextScope();
            var tokens = new Mock<IAuthenticationTokenCreator>();
            tokens.Setup(t => t.CreateToken(It.IsAny<UserPayload>())).ReturnsAsync("NEW.ACCESS.TOKEN");
            return (new RefreashTokenHandler(scope.Context, tokens.Object,
                NullLogger<RefreashTokenHandler>.Instance), scope.Context, scope, tokens);
        }

        private static User SeedUserWithRole(AuthDbContext ctx, string email)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = "User", Description = "U", CreatedAt = DateTime.UtcNow };
            ctx.Roles.Add(role);
            var u = new User
            {
                Id = Guid.NewGuid(), Email = email, Username = "u_" + Guid.NewGuid().ToString("N")[..6],
                PasswordHash = "h", Status = Status.Active, AuthProvider = AuthProvider.Local,
                IsEmailVerified = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            ctx.Users.Add(u);
            ctx.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
            ctx.SaveChanges();
            return u;
        }

        [Fact]
        public async Task Returns_USER_NOT_FOUND_when_no_user_for_email()
        {
            var (h, _, scope, _) = Build();
            await using var _s = scope;
            var res = await h.Handle(new RefreashTokenCommand("ghost@x.com", "rt"), default);
            res.Error!.Title.Should().Be("USER_NOT_FOUND");
            res.Error.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task Returns_REFRESH_TOKEN_NOT_FOUND_when_no_active_refresh_token_exists()
        {
            var (h, ctx, scope, _) = Build();
            await using var _s = scope;
            SeedUserWithRole(ctx, "u@x.com");

            var res = await h.Handle(new RefreashTokenCommand("u@x.com", "rt"), default);

            res.Error!.Title.Should().Be("REFRESH_TOKEN_NOT_FOUND");
        }

        [Fact]
        public async Task Returns_REFRESH_TOKEN_NOT_FOUND_when_token_is_expired()
        {
            var (h, ctx, scope, _) = Build();
            await using var _s = scope;
            var u = SeedUserWithRole(ctx, "u@x.com");
            ctx.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(), UserId = u.Id, TokenHash = "H",
                CreatedAt = DateTime.UtcNow.AddDays(-30), ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });
            ctx.SaveChanges();

            var res = await h.Handle(new RefreashTokenCommand("u@x.com", "rt"), default);

            res.Error!.Title.Should().Be("REFRESH_TOKEN_NOT_FOUND");
        }

        [Fact]
        public async Task Returns_INVALID_REFRESH_TOKEN_when_token_hash_mismatch()
        {
            var (h, ctx, scope, _) = Build();
            await using var _s = scope;
            var u = SeedUserWithRole(ctx, "u@x.com");
            var realToken = await RefreashTokenGenerator.CreateTokenAsync();
            var realHash  = await RefreashTokenGenerator.HashTokenAsync(realToken);
            ctx.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(), UserId = u.Id, TokenHash = realHash,
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            ctx.SaveChanges();

            var res = await h.Handle(new RefreashTokenCommand("u@x.com", "tampered-token"), default);

            res.Error!.Title.Should().Be("INVALID_REFRESH_TOKEN");
            res.Error.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task Returns_new_access_token_when_token_matches()
        {
            var (h, ctx, scope, _) = Build();
            await using var _s = scope;
            var u = SeedUserWithRole(ctx, "u@x.com");
            var realToken = await RefreashTokenGenerator.CreateTokenAsync();
            var realHash  = await RefreashTokenGenerator.HashTokenAsync(realToken);
            ctx.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(), UserId = u.Id, TokenHash = realHash,
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            ctx.SaveChanges();

            var res = await h.Handle(new RefreashTokenCommand("u@x.com", realToken), default);

            res.Error.Should().BeNull();
            res.Result!.AccessToken.Should().Be("NEW.ACCESS.TOKEN");
        }

        [Fact]
        public async Task Handle_accepts_any_active_refresh_token_a_user_owns()
        {
            var (h, ctx, scope, _) = Build();
            await using var _s = scope;
            var u = SeedUserWithRole(ctx, "u@x.com");

            var tokenA = await RefreashTokenGenerator.CreateTokenAsync();
            var hashA  = await RefreashTokenGenerator.HashTokenAsync(tokenA);
            var tokenB = await RefreashTokenGenerator.CreateTokenAsync();
            var hashB  = await RefreashTokenGenerator.HashTokenAsync(tokenB);

            ctx.RefreshTokens.AddRange(
                new RefreshToken { Id = Guid.NewGuid(), UserId = u.Id, TokenHash = hashA,
                                   CreatedAt = DateTime.UtcNow.AddDays(-1), ExpiresAt = DateTime.UtcNow.AddDays(7) },
                new RefreshToken { Id = Guid.NewGuid(), UserId = u.Id, TokenHash = hashB,
                                   CreatedAt = DateTime.UtcNow,            ExpiresAt = DateTime.UtcNow.AddDays(7) });
            ctx.SaveChanges();

            var resB = await h.Handle(new RefreashTokenCommand("u@x.com", tokenB), default);
            var resA = await h.Handle(new RefreashTokenCommand("u@x.com", tokenA), default);

            resA.Error.Should().BeNull();
            resB.Error.Should().BeNull();
        }
    }
}
