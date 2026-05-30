using BuildingBlocks.Messaging.KafkaLogger;
using MongoDB.Driver;
using QueryService.API.Repository;

namespace QueryService.API.Apis.Logs.CountByService
{
    public record CountByServiceQuery(
        string? LogLevel,
        DateTime? FromDate,
        DateTime? ToDate) : IQuery<CountByServiceResult>;

    public record CountByServiceResult(List<ServiceCountItem>? Items, long TotalCount, ErrorCarrier? Error);

    public record ServiceCountItem(string? ServiceName, long Count);

    public class CountByServiceHandler : IQueryHandler<CountByServiceQuery, CountByServiceResult>
    {
        private readonly LogQueryRepository _repository;
        private readonly ILogger<CountByServiceHandler> _logger;

        public CountByServiceHandler(LogQueryRepository repository, ILogger<CountByServiceHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<CountByServiceResult> Handle(CountByServiceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var filterBuilder = Builders<LogModel>.Filter;
                var filters = new List<FilterDefinition<LogModel>>();

                if (!string.IsNullOrWhiteSpace(request.LogLevel))
                    filters.Add(filterBuilder.Eq(x => x.LogLevel, request.LogLevel));

                if (request.FromDate.HasValue)
                    filters.Add(filterBuilder.Gte(x => x.Timestamp, request.FromDate.Value));

                if (request.ToDate.HasValue)
                    filters.Add(filterBuilder.Lte(x => x.Timestamp, request.ToDate.Value));

                var filter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;

                var buckets = await _repository.CountByServiceNameAsync(filter, cancellationToken);

                var items = buckets.Select(b => new ServiceCountItem(b.Key, b.Count)).ToList();
                var total = items.Sum(i => i.Count);

                _logger.LogInformation("CountByService returned {Buckets} bucket(s), total {Total}", items.Count, total);

                return new CountByServiceResult(items, total, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while counting logs by service");
                return new CountByServiceResult(null, 0, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request. Please try again later."
                });
            }
        }
    }
}
