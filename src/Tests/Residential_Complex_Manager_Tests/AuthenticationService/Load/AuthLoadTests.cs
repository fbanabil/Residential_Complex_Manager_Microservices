extern alias AuthApi;
using AuthApi::AuthenticationService.API.Apis.User.LocalLogin;
using AuthApi::AuthenticationService.API.EntityModels;
using AuthApi::AuthenticationService.API.Enum;
using AuthApi::AuthenticationService.API.Helpers.Authenticate;
using AuthApi::AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using AuthApi::AuthenticationService.API.Helpers.VerificationToken;
using Microsoft.Extensions.Logging.Abstractions;
using NBomber.CSharp;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Load
{
    /// <summary>
    /// Sustained-load tests using NBomber. Each scenario maintains a *steady* number of
    /// concurrent virtual users for a fixed window and asserts the handler still responds
    /// (no exceptions, no error results).
    /// </summary>
    public class AuthLoadTests
    {
        [Fact]
        public async Task LocalLogin_handler_sustains_concurrent_load_for_15_seconds()
        {
            await using var scope = new SqliteAuthDbContextScope();
            var ctx = scope.Context;
            var config = TestConfigurationFactory.BuildAuthConfiguration();
            var hasher = new PasswordHasher(config);
            var tokens = new AuthenticationTokenCreator(config);
            var verif  = new VerificationTokenGenerator();

            var role = new Role { Id = Guid.NewGuid(), Name = "User", Description = "U", CreatedAt = DateTime.UtcNow };
            var user = new User
            {
                Id = Guid.NewGuid(), Email = "load@x.com", Username = "load",
                PasswordHash = await hasher.HashPassword("Sup3r!Strong"),
                Status = Status.Active, AuthProvider = AuthProvider.Local,
                IsEmailVerified = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            ctx.Roles.Add(role);
            ctx.Users.Add(user);
            ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var handler = new LocalLoginHandler(ctx, tokens, verif, hasher,
                NullLogger<LocalLoginHandler>.Instance);

            // We serialise through a SemaphoreSlim because SQLite + DbContext is not
            // thread-safe â€” load is exerted by *rate of issuance*, not parallel DB writes.
            var dbGate = new SemaphoreSlim(1, 1);

            var scenario = Scenario.Create("local_login_load", async ctxNb =>
            {
                await dbGate.WaitAsync();
                try
                {
                    var r = await handler.Handle(new LocalLoginCommand("load@x.com", "Sup3r!Strong"), default);
                    return r.Error is null ? Response.Ok() : Response.Fail(message: r.Error.Title ?? "fail");
                }
                catch (Exception ex) { return Response.Fail(message: ex.Message); }
                finally { dbGate.Release(); }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(15)));

            var stats = NBomberRunner.RegisterScenarios(scenario)
                                     .WithReportFolder("nbomber-reports/auth-load")
                                     .Run();

            var s = stats.AllRequestCount;
            stats.AllOkCount.Should().BeGreaterThan(0);
            stats.AllFailCount.Should().Be(0, "no login attempts should fail under sustained baseline load");
        }
    }
}
