using NBomber.CSharp;
using QueryService.API.Apis.Logs.FilterLogs;

namespace Residential_Complex_Manager_Tests.QueryService.Stress
{
    [Trait("Category", "Stress")]
    public class QueryStressTests
    {
        [Fact]
        public void FilterLogsValidator_under_burst_traffic_keeps_failure_rate_at_zero_for_valid_input()
        {
            var v = new FilterLogsRequestValidator();

            var scenario = Scenario.Create("filter_logs_validation_stress", async _ =>
            {
                var req = new FilterLogsRequest("svc", "Information", "msg", null, null, null,
                    null, null, null, null, 1, 20, "timestamp", "desc");
                var r = await v.ValidateAsync(req);
                return r.IsValid ? Response.Ok() : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.RampingInject(rate: 2000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
                Simulation.Inject(rate: 2000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)));

            var stats = NBomberRunner.RegisterScenarios(scenario)
                                     .WithReportFolder("nbomber-reports/query-stress")
                                     .Run();

            stats.AllRequestCount.Should().BeGreaterThan(0);
            stats.AllFailCount.Should().Be(0, "the validator is pure CPU and must not fail under stress");
        }
    }
}
