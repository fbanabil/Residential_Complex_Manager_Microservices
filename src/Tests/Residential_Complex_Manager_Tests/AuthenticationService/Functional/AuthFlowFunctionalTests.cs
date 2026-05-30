extern alias AuthApi;
using AuthApi::AuthenticationService.API.Apis.User.ChangePassword;
using AuthApi::AuthenticationService.API.Apis.User.LocalLogin;
using AuthApi::AuthenticationService.API.Apis.User.RefreashToken;
using AuthApi::AuthenticationService.API.Apis.User.VerifyUserEmail;
using AuthApi::AuthenticationService.API.EntityModels;
using AuthApi::AuthenticationService.API.Enum;
using AuthApi::AuthenticationService.API.Helpers.Authenticate;
using AuthApi::AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using AuthApi::AuthenticationService.API.Helpers.RefreashTokenHelper;
using AuthApi::AuthenticationService.API.Helpers.VerificationToken;
using Microsoft.Extensions.Logging.Abstractions;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Functional
{
    /// <summary>
    /// Black-box functional tests that drive real workflows end to end through the
    /// handlers: register â†’ verify email â†’ login â†’ change password â†’ refresh token.
    /// We compose handlers manually (rather than spinning up a full WebApplicationFactory)
    /// because the production Program.cs requires SQL Server and external services that
    /// cannot be conjured in a unit-test process.
    /// </summary>
    public class AuthFlowFunctionalTests
    {
        [Fact]
        public async Task Verify_email_then_login_then_change_password_then_refresh_token_happy_path()
        {
            await using var scope = new SqliteAuthDbContextScope();
            var ctx = scope.Context;
            var config = TestConfigurationFactory.BuildAuthConfiguration();
            var hasher = new PasswordHasher(config);
            var tokens = new AuthenticationTokenCreator(config);
            var verif  = new VerificationTokenGenerator();

            // ---- seed user that has been "registered" (still email-unverified) ----
            var role = new Role { Id = Guid.NewGuid(), Name = "User", Description = "U", CreatedAt = DateTime.UtcNow };
            var user = new User
            {
                Id = Guid.NewGuid(), Email = "flow@x.com", Username = "flow",
                PasswordHash = await hasher.HashPassword("Initi@l!Pwd1"),
                Status = Status.Active, AuthProvider = AuthProvider.Local,
                IsEmailVerified = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            ctx.Roles.Add(role);
            ctx.Users.Add(user);
            ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });

            // simulate the registration-issued verification token
            var raw  = await verif.GenerateTokenAsync();
            var hash = await verif.HashTokenAsync(raw);
            ctx.SecurityTokens.Add(new SecurityTokens
            {
                Id = Guid.NewGuid(), UserId = user.Id, Token = hash,
                Type = TokenType.EmailConfirmation, ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            });
            await ctx.SaveChangesAsync();

            // ---- 1. verify email ----
            var verifyHandler = new VerifyUserEmailHandler(ctx, verif, NullLogger<VerifyUserEmailHandler>.Instance);
            var verifyResult = await verifyHandler.Handle(new VerifyUserEmailCommand(user.Id, raw), default);
            verifyResult.ErrorCarrier.Should().BeNull();
            // ExecuteUpdate bypasses the change tracker, so the tracked User instance still
            // shows IsEmailVerified=false. Drop tracked state before the next handler.
            ctx.ChangeTracker.Clear();

            // ---- 2. login should now succeed and issue tokens ----
            var loginHandler = new LocalLoginHandler(ctx, tokens, verif, hasher,
                NullLogger<LocalLoginHandler>.Instance);
            var login = await loginHandler.Handle(new LocalLoginCommand("flow@x.com", "Initi@l!Pwd1"), default);
            login.Error.Should().BeNull();
            login.Result!.AccessToken.Should().NotBeNullOrEmpty();
            var issuedRefresh = login.Result.RefreshToken!;

            // ---- 3. change password ----
            var changeHandler = new ChangePasswordHandler(ctx, hasher, NullLogger<ChangePasswordHandler>.Instance);
            var change = await changeHandler.Handle(new ChangePasswordCommand(
                "Initi@l!Pwd1", "N3w!Strong#1", "N3w!Strong#1", "flow@x.com"), default);
            change.Error.Should().BeNull();

            // ---- 4. login again with new password ----
            ctx.ChangeTracker.Clear();
            var loginNew = await loginHandler.Handle(new LocalLoginCommand("flow@x.com", "N3w!Strong#1"), default);
            loginNew.Error.Should().BeNull();

            // ---- 5. refresh token using freshly-issued refresh ----
            var refreshIssued = loginNew.Result!.RefreshToken!;
            var refreshHandler = new RefreashTokenHandler(ctx, tokens,
                NullLogger<RefreashTokenHandler>.Instance);
            var refresh = await refreshHandler.Handle(new RefreashTokenCommand("flow@x.com", refreshIssued), default);
            refresh.Error.Should().BeNull();
            refresh.Result!.AccessToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_is_rejected_when_email_unverified_and_succeeds_after_verification()
        {
            await using var scope = new SqliteAuthDbContextScope();
            var ctx = scope.Context;
            var config = TestConfigurationFactory.BuildAuthConfiguration();
            var hasher = new PasswordHasher(config);
            var tokens = new AuthenticationTokenCreator(config);
            var verif  = new VerificationTokenGenerator();
            var user = new User
            {
                Id = Guid.NewGuid(), Email = "gate@x.com", Username = "gate",
                PasswordHash = await hasher.HashPassword("Sup3r!1"),
                Status = Status.Active, AuthProvider = AuthProvider.Local,
                IsEmailVerified = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var loginHandler = new LocalLoginHandler(ctx, tokens, verif, hasher,
                NullLogger<LocalLoginHandler>.Instance);
            var first = await loginHandler.Handle(new LocalLoginCommand("gate@x.com", "Sup3r!1"), default);
            first.Error!.Title.Should().Be("EMAIL_NOT_VERIFIED");

            // Now mark verified directly (simulating successful email verification)
            user.IsEmailVerified = true;
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var second = await loginHandler.Handle(new LocalLoginCommand("gate@x.com", "Sup3r!1"), default);
            second.Error.Should().BeNull();
        }
    }
}
