extern alias AuthApi;
using AuthApi::AuthenticationService.API.Apis.User.LocalLogin;
using AuthApi::AuthenticationService.API.EntityModels;
using AuthApi::AuthenticationService.API.Enum;
using AuthApi::AuthenticationService.API.Helpers.Authenticate;
using AuthApi::AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using AuthApi::AuthenticationService.API.Helpers.VerificationToken;
using Microsoft.Extensions.Logging.Abstractions;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Integration
{
    /// <summary>
    /// Integration tests exercising the real PasswordHasher + AuthenticationTokenCreator +
    /// real SQLite database. Only the email helper / verification token generator are stubbed.
    /// </summary>
    public class LocalLoginIntegrationTests
    {
        [Fact]
        public async Task End_to_end_login_with_real_password_hasher_and_token_creator_succeeds()
        {
            await using var scope = new SqliteAuthDbContextScope();
            var ctx = scope.Context;
            var config = TestConfigurationFactory.BuildAuthConfiguration();
            var hasher = new PasswordHasher(config);
            var tokens = new AuthenticationTokenCreator(config);

            var role = new Role { Id = Guid.NewGuid(), Name = "User", Description = "U", CreatedAt = DateTime.UtcNow };
            var user = new User
            {
                Id = Guid.NewGuid(), Email = "real@x.com", Username = "real",
                PasswordHash = await hasher.HashPassword("Sup3r!Strong"),
                Status = Status.Active, AuthProvider = AuthProvider.Local,
                IsEmailVerified = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            ctx.Roles.Add(role);
            ctx.Users.Add(user);
            ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var handler = new LocalLoginHandler(ctx, tokens, new VerificationTokenGenerator(),
                hasher, NullLogger<LocalLoginHandler>.Instance);

            var result = await handler.Handle(new LocalLoginCommand("real@x.com", "Sup3r!Strong"), default);

            result.Error.Should().BeNull();
            result.Result!.AccessToken.Should().NotBeNullOrEmpty();
            result.Result.RefreshToken.Should().NotBeNullOrEmpty();
            ctx.RefreshTokens.Should().ContainSingle(rt => rt.UserId == user.Id && rt.RevokedAt == null);
        }
    }
}
