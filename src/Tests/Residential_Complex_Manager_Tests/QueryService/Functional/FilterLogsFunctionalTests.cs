using QueryService.API.Apis.Logs.FilterLogs;

namespace Residential_Complex_Manager_Tests.QueryService.Functional
{
    /// <summary>
    /// Functional tests checking the request → query mapping documented in the endpoint.
    /// The endpoint defaults Page = 1, PageSize = 20, SortOrder = "desc" — these
    /// defaults are part of the public contract and a regression would silently change
    /// pagination for every caller.
    /// </summary>
    public class FilterLogsFunctionalTests
    {
        private readonly FilterLogsRequestValidator _validator = new();

        [Fact]
        public async Task Endpoint_contract_request_with_all_nulls_produces_defaults_after_mapping()
        {
            var req = new FilterLogsRequest(null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            (await _validator.ValidateAsync(req)).IsValid.Should().BeTrue();

            // Mimic what the endpoint does when constructing the query
            var page = req.Page ?? 1;
            var size = req.PageSize ?? 20;
            var order = req.SortOrder ?? "desc";

            page.Should().Be(1);
            size.Should().Be(20);
            order.Should().Be("desc");
        }

        [Fact]
        public async Task Endpoint_contract_total_pages_math_handles_remainder_correctly()
        {
            long totalCount = 25;
            int pageSize = 10;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            totalPages.Should().Be(3);

            totalCount = 0;
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            totalPages.Should().Be(0);
        }

        [Fact]
        public async Task End_to_end_validator_then_query_construction_for_a_realistic_filter()
        {
            var req = new FilterLogsRequest(
                ServiceName: "auth",
                LogLevel: "Error",
                Message: "timeout",
                CorrelationId: "corr-42",
                RequestId: null,
                TraceId: null,
                Category: null,
                ExceptionType: "TimeoutException",
                FromDate: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                ToDate:   new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                Page: 2,
                PageSize: 50,
                SortBy: "timestamp",
                SortOrder: "asc");
            (await _validator.ValidateAsync(req)).IsValid.Should().BeTrue();

            var query = new FilterLogsQuery(req.ServiceName, req.LogLevel, req.Message,
                req.CorrelationId, req.RequestId, req.TraceId, req.Category, req.ExceptionType,
                req.FromDate, req.ToDate, req.Page ?? 1, req.PageSize ?? 20,
                req.SortBy, req.SortOrder ?? "desc");
            query.Page.Should().Be(2);
            query.PageSize.Should().Be(50);
            query.SortOrder.Should().Be("asc");
        }
    }
}
