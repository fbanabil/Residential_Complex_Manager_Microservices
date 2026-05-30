using QueryService.API.Apis.Logs.FilterLogs;
using System.Diagnostics;

namespace Residential_Complex_Manager_Tests.QueryService.Performance
{
    public class QueryPerformanceTests
    {
        [Fact]
        public async Task FilterLogsValidator_handles_10000_requests_under_three_seconds()
        {
            var v = new FilterLogsRequestValidator();
            var req = new FilterLogsRequest("svc", "Error", null, null, null, null,
                null, null, null, null, 1, 20, "timestamp", "desc");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10_000; i++) await v.ValidateAsync(req);
            sw.Stop();

            sw.Elapsed.TotalSeconds.Should().BeLessThan(3,
                "validators are pure CPU and must be cheap — 10k iterations should be well under three seconds");
        }
    }
}
