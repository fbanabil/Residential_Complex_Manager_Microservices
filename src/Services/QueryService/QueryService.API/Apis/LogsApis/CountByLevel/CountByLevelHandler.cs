using BuildingBlocks.Messaging.KafkaLogger;
using MongoDB.Driver;
using QueryService.API.Repository;

namespace QueryService.API.Apis.Logs.CountByLevel
{
    public record CountByLevelQuery(
        string? ServiceName,
        DateTime? FromDate,
        DateTime? ToDate) : IQuery<CountByLevelResult>;

    public record CountByLevelResult(List<LevelCountItem>? Items, long TotalCount, ErrorCarrier? Error);

    public record LevelCountItem(string? LogLevel, long Count);

    public class CountByLevelHandler : IQueryHandler<CountByLevelQuery, CountByLevelResult>
    {
        private readonly LogQueryRepository _repository;
        private readonly ILogger<CountByLevelHandler> _logger;

        public CountByLevelHandler(LogQueryRepository repository, ILogger<CountByLevelHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<CountByLevelResult> Handle(CountByLevelQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var filterBuilder = Builders<LogModel>.Filter;
                var filters = new List<FilterDefinition<LogModel>>();

                if (!string.IsNullOrWhiteSpace(request.ServiceName))
                    filters.Add(filterBuilder.Eq(x => x.ServiceName, request.ServiceName));

                if (request.FromDate.HasValue)
                    filters.Add(filterBuilder.Gte(x => x.Timestamp, request.FromDate.Value));

                if (request.ToDate.HasValue)
                    filters.Add(filterBuilder.Lte(x => x.Timestamp, request.ToDate.Value));

                var filter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;

                var buckets = await _repository.CountByLogLevelAsync(filter, cancellationToken);

                var items = buckets.Select(b => new LevelCountItem(b.Key, b.Count)).ToList();
                var total = items.Sum(i => i.Count);

                _logger.LogInformation("CountByLevel returned {Buckets} bucket(s), total {Total}", items.Count, total);

                return new CountByLevelResult(items, total, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while counting logs by level");
                return new CountByLevelResult(null, 0, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request. Please try again later."
                });
            }
        }
    }
}
