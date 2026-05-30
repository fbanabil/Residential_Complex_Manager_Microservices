using QueryService.API.Helpers.ErrorCarrier;
using QueryService.API.Apis.Logs.CountByLevel;
using QueryService.API.Apis.Logs.FilterLogs;
using QueryService.API.Apis.Logs.GetLogById;
using QueryService.API.Apis.Logs.GetLogsByCorrelationId;
using QueryService.API.Apis.Logs.GetServiceNames;
using QueryService.API.Apis.Logs.TimeSeries;

namespace Residential_Complex_Manager_Tests.QueryService.Unit
{
    /// <summary>
    /// Guards the public shape of the query and result record types. A breaking change
    /// to any record signature trips a failing test here before the consumers fail.
    /// </summary>
    public class HandlerShapeTests
    {
        [Fact]
        public void FilterLogsQuery_carries_all_paging_and_filter_fields()
        {
            var q = new FilterLogsQuery("svc", "Error", "msg", "cid", "rid", "tid",
                "cat", "ex", DateTime.UtcNow, DateTime.UtcNow, 1, 20, "timestamp", "desc");
            q.Page.Should().Be(1);
            q.PageSize.Should().Be(20);
            q.SortBy.Should().Be("timestamp");
            q.SortOrder.Should().Be("desc");
        }

        [Fact]
        public void FilterLogsResult_can_express_success_and_error_branches()
        {
            new FilterLogsResult(new List<LogItemResponse>(), 0, 1, 20, null)
                .Error.Should().BeNull();
            new FilterLogsResult(null, 0, 1, 20, new ErrorCarrier { StatusCode = 500 })
                .Logs.Should().BeNull();
        }

        [Fact]
        public void GetLogByIdQuery_id_is_required_and_round_trips()
        {
            new GetLogByIdQuery("abc").Id.Should().Be("abc");
        }

        [Fact]
        public void CountByLevelQuery_records_filters()
        {
            var q = new CountByLevelQuery("svc", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            q.ServiceName.Should().Be("svc");
            q.FromDate.Should().NotBeNull();
            q.ToDate.Should().NotBeNull();
        }

        [Fact]
        public void GetLogsByCorrelationIdQuery_record_round_trip()
        {
            new GetLogsByCorrelationIdQuery("corr-1").CorrelationId.Should().Be("corr-1");
        }

        [Fact]
        public void GetServiceNamesQuery_has_default_constructor()
        {
            // The query record has no parameters — a regression here would break the API surface.
            var q = new GetServiceNamesQuery();
            q.Should().NotBeNull();
        }

        [Fact]
        public void TimeSeriesQuery_bucket_unit_is_first_positional_parameter()
        {
            var q = new TimeSeriesQuery("hour", null, null, null, null);
            q.Bucket.Should().Be("hour");
        }
    }
}
