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

namespace Residential_Complex_Manager_Tests.AuthenticationService.Stress
{
    /// <summary>
    /// Stress tests push past nominal load to look for breakage. Unlike load tests we
    /// expect SOME failures here â€” the assertion is that the system *recovers* and that
    /// failure rate stays below a ceiling. These don't run by default in CI; tag with
    /// 'Stress' so they can be selected explicitly.
    /// </summary>
    [Trait("Category", "Stress")]
    public class AuthStressTests
    {
        [Fact]
        public async Task LocalLogin_handler_under_burst_traffic_keeps_failure_rate_below_5_percent()
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
                Id = Guid.NewGuid(), Email = "stress@x.com", Username = "stress",
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

            // Single-writer DbContext: serialise calls. Stress comes from the requested
            // *rate* (Inject) rather than from parallel DB access.
            var dbGate = new SemaphoreSlim(1, 1);

            var scenario = Scenario.Create("local_login_stress", async _ =>
            {
                await dbGate.WaitAsync();
                try
                {
                    var r = await handler.Handle(new LocalLoginCommand("stress@x.com", "Sup3r!Strong"), default);
                    return r.Error is null ? Response.Ok() : Response.Fail(message: r.Error.Title ?? "fail");
                }
                catch (Exception ex) { return Response.Fail(message: ex.Message); }
                finally { dbGate.Release(); }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.RampingInject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
                Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)));

            var stats = NBomberRunner.RegisterScenarios(scenario)
                                     .WithReportFolder("nbomber-reports/auth-stress")
                                     .Run();

            var total = stats.AllRequestCount;
            total.Should().BeGreaterThan(0);
            var failureRate = total == 0 ? 0 : (double)stats.AllFailCount / total;
            failureRate.Should().BeLessThan(0.05,
                "even under stress the login pipeline should keep its failure rate under 5%");
        }
    }
}
