using NBomber.CSharp;
using QueryService.API.Apis.Logs.FilterLogs;

namespace Residential_Complex_Manager_Tests.QueryService.Load
{
    public class QueryLoadTests
    {
        [Fact]
        public void FilterLogsValidator_can_sustain_concurrent_load()
        {
            var v = new FilterLogsRequestValidator();

            var scenario = Scenario.Create("filter_logs_validation_load", async _ =>
            {
                var req = new FilterLogsRequest("svc", "Information", "msg", null, null, null,
                    null, null, null, null, 1, 20, "timestamp", "desc");
                var r = await v.ValidateAsync(req);
                return r.IsValid ? Response.Ok() : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(15)));

            var stats = NBomberRunner.RegisterScenarios(scenario)
                                     .WithReportFolder("nbomber-reports/query-load")
                                     .Run();

            stats.AllOkCount.Should().BeGreaterThan(0);
            stats.AllFailCount.Should().Be(0);
        }
    }
}
