using BuildingBlocks.Messaging.KafkaLogger;
using MongoDB.Driver;
using QueryService.API.Repository;

namespace QueryService.API.Apis.Logs.TimeSeries
{
    public record TimeSeriesQuery(
        string Bucket,
        string? ServiceName,
        string? LogLevel,
        DateTime? FromDate,
        DateTime? ToDate) : IQuery<TimeSeriesResult>;

    public record TimeSeriesResult(List<TimeSeriesPoint>? Points, ErrorCarrier? Error);

    public record TimeSeriesPoint(DateTime Bucket, string? LogLevel, long Count);

    public class TimeSeriesHandler : IQueryHandler<TimeSeriesQuery, TimeSeriesResult>
    {
        private readonly LogQueryRepository _repository;
        private readonly ILogger<TimeSeriesHandler> _logger;

        public TimeSeriesHandler(LogQueryRepository repository, ILogger<TimeSeriesHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<TimeSeriesResult> Handle(TimeSeriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var filterBuilder = Builders<LogModel>.Filter;
                var filters = new List<FilterDefinition<LogModel>>();

                if (!string.IsNullOrWhiteSpace(request.ServiceName))
                    filters.Add(filterBuilder.Eq(x => x.ServiceName, request.ServiceName));

                if (!string.IsNullOrWhiteSpace(request.LogLevel))
                    filters.Add(filterBuilder.Eq(x => x.LogLevel, request.LogLevel));

                if (request.FromDate.HasValue)
                    filters.Add(filterBuilder.Gte(x => x.Timestamp, request.FromDate.Value));

                if (request.ToDate.HasValue)
                    filters.Add(filterBuilder.Lte(x => x.Timestamp, request.ToDate.Value));

                var filter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;

                var buckets = await _repository.CountByTimeBucketAsync(filter, request.Bucket, cancellationToken);

                var points = buckets.Select(b => new TimeSeriesPoint(b.Bucket, b.LogLevel, b.Count)).ToList();

                _logger.LogInformation("TimeSeries returned {Count} point(s) at {Bucket} granularity", points.Count, request.Bucket);

                return new TimeSeriesResult(points, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while building log time series");
                return new TimeSeriesResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request. Please try again later."
                });
            }
        }
    }
}
