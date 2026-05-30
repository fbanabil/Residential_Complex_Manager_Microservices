using QueryService.API.Apis.Logs.FilterLogs;

namespace Residential_Complex_Manager_Tests.QueryService.Unit
{
    public class FilterLogsRequestValidatorTests
    {
        private readonly FilterLogsRequestValidator _sut = new();

        private static FilterLogsRequest Empty() =>
            new(null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        [Fact]
        public async Task Empty_request_is_valid()
        {
            var result = await _sut.ValidateAsync(Empty());
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Trace")]
        [InlineData("Debug")]
        [InlineData("Information")]
        [InlineData("Warning")]
        [InlineData("Error")]
        [InlineData("Critical")]
        [InlineData("INFORMATION")] // case-insensitive
        [InlineData("warning")]
        public async Task Accepts_known_log_levels(string level)
        {
            var result = await _sut.ValidateAsync(Empty() with { LogLevel = level });
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Bogus")]
        [InlineData("Verbose")]
        [InlineData("Fatal")]
        public async Task Rejects_unknown_log_levels(string level)
        {
            var result = await _sut.ValidateAsync(Empty() with { LogLevel = level });
            result.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Rejects_non_positive_page(int page)
        {
            var result = await _sut.ValidateAsync(Empty() with { Page = page });
            result.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        [InlineData(int.MaxValue)]
        public async Task Rejects_out_of_range_page_size(int pageSize)
        {
            var result = await _sut.ValidateAsync(Empty() with { PageSize = pageSize });
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Accepts_page_size_at_max()
        {
            var result = await _sut.ValidateAsync(Empty() with { PageSize = 100, Page = 1 });
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("timestamp")]
        [InlineData("loglevel")]
        [InlineData("servicename")]
        public async Task Accepts_known_sort_keys(string key)
        {
            var result = await _sut.ValidateAsync(Empty() with { SortBy = key });
            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("Timestamp")]
        [InlineData("id")]
        [InlineData("message")]
        public async Task Rejects_unknown_sort_keys(string key)
        {
            var result = await _sut.ValidateAsync(Empty() with { SortBy = key });
            result.IsValid.Should().BeFalse(
                "the SortBy rule is case-sensitive and only accepts a strict 3-value enum");
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("desc")]
        public async Task Accepts_known_sort_orders(string order)
        {
            var result = await _sut.ValidateAsync(Empty() with { SortOrder = order });
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Rejects_FromDate_after_ToDate()
        {
            var result = await _sut.ValidateAsync(Empty() with
            {
                FromDate = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc),
                ToDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
            });
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Accepts_FromDate_equal_to_ToDate()
        {
            var d = new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc);
            var result = await _sut.ValidateAsync(Empty() with { FromDate = d, ToDate = d });
            result.IsValid.Should().BeTrue();
        }
    }
}
