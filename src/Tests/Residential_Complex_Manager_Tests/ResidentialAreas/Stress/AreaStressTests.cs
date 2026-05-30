using NBomber.CSharp;
using ResidentialAreas.API.Helpers.Image;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Stress
{
    [Trait("Category", "Stress")]
    public class AreaStressTests
    {
        [Fact]
        public void Base64StringImageValidator_under_extreme_burst_keeps_zero_failures()
        {
            var input = TestConfigurationFactory.ValidBase64Png;

            var scenario = Scenario.Create("base64_validator_stress", async _ =>
            {
                var ok = Base64StringImageValidator.IsBase64StringImage(input);
                await Task.CompletedTask;
                return ok ? Response.Ok() : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.RampingInject(rate: 5000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
                Simulation.Inject(rate: 5000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)));

            var stats = NBomberRunner.RegisterScenarios(scenario)
                                     .WithReportFolder("nbomber-reports/area-stress")
                                     .Run();

            stats.AllRequestCount.Should().BeGreaterThan(0);
            stats.AllFailCount.Should().Be(0);
        }
    }
}
