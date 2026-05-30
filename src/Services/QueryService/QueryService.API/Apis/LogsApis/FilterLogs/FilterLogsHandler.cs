using BuildingBlocks.Messaging.KafkaLogger;
using MongoDB.Driver;
using QueryService.API.Repository;

namespace QueryService.API.Apis.Logs.FilterLogs
{
    public record FilterLogsQuery(
        string? ServiceName,
        string? LogLevel,
        string? Message,
        string? CorrelationId,
        string? RequestId,
        string? TraceId,
        string? Category,
        string? ExceptionType,
        DateTime? FromDate,
        DateTime? ToDate,
        int Page,
        int PageSize,
        string? SortBy,
        string? SortOrder) : IQuery<FilterLogsResult>;

    public record FilterLogsResult(List<LogItemResponse>? Logs, long TotalCount, int Page, int PageSize, ErrorCarrier? Error);

    public record LogItemResponse(
        string Id,
        string? ServiceName,
        string? Environment,
        string? LogLevel,
        string? Message,
        DateTime Timestamp,
        string? ExceptionType,
        string? ExceptionMessage,
        string? CorrelationId,
        string? RequestId,
        string? TraceId,
        string? SpanId,
        string? Category,
        int EventId,
        string? EventName,
        Dictionary<string, string?> Properties);

    public class FilterLogsHandler : IQueryHandler<FilterLogsQuery, FilterLogsResult>
    {
        private readonly LogQueryRepository _repository;
        private readonly ILogger<FilterLogsHandler> _logger;

        public FilterLogsHandler(LogQueryRepository repository, ILogger<FilterLogsHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<FilterLogsResult> Handle(FilterLogsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var filterBuilder = Builders<LogModel>.Filter;
                var filters = new List<FilterDefinition<LogModel>>();

                if (!string.IsNullOrWhiteSpace(request.ServiceName))
                    filters.Add(filterBuilder.Eq(x => x.ServiceName, request.ServiceName));

                if (!string.IsNullOrWhiteSpace(request.LogLevel))
                    filters.Add(filterBuilder.Eq(x => x.LogLevel, request.LogLevel));

                if (!string.IsNullOrWhiteSpace(request.Message))
                    filters.Add(filterBuilder.Regex(x => x.Message, new MongoDB.Bson.BsonRegularExpression(request.Message, "i")));

                if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                    filters.Add(filterBuilder.Eq(x => x.CorrelationId, request.CorrelationId));

                if (!string.IsNullOrWhiteSpace(request.RequestId))
                    filters.Add(filterBuilder.Eq(x => x.RequestId, request.RequestId));

                if (!string.IsNullOrWhiteSpace(request.TraceId))
                    filters.Add(filterBuilder.Eq(x => x.TraceId, request.TraceId));

                if (!string.IsNullOrWhiteSpace(request.Category))
                    filters.Add(filterBuilder.Regex(x => x.Category, new MongoDB.Bson.BsonRegularExpression(request.Category, "i")));

                if (!string.IsNullOrWhiteSpace(request.ExceptionType))
                    filters.Add(filterBuilder.Eq(x => x.ExceptionType, request.ExceptionType));

                if (request.FromDate.HasValue)
                    filters.Add(filterBuilder.Gte(x => x.Timestamp, request.FromDate.Value));

                if (request.ToDate.HasValue)
                    filters.Add(filterBuilder.Lte(x => x.Timestamp, request.ToDate.Value));

                var filter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;

                var sortBuilder = Builders<LogModel>.Sort;
                var sort = request.SortBy?.ToLowerInvariant() switch
                {
                    "loglevel" => request.SortOrder == "asc" ? sortBuilder.Ascending(x => x.LogLevel) : sortBuilder.Descending(x => x.LogLevel),
                    "servicename" => request.SortOrder == "asc" ? sortBuilder.Ascending(x => x.ServiceName) : sortBuilder.Descending(x => x.ServiceName),
                    _ => request.SortOrder == "asc" ? sortBuilder.Ascending(x => x.Timestamp) : sortBuilder.Descending(x => x.Timestamp)
                };

                var totalCount = await _repository.CountLogsAsync(filter, cancellationToken);
                var logs = await _repository.FilterLogsAsync(filter, sort, request.Page, request.PageSize, cancellationToken);

                var logItems = logs.Select(l => new LogItemResponse(
                    l.Id, l.ServiceName, l.Environment, l.LogLevel, l.Message,
                    l.Timestamp, l.ExceptionType, l.ExceptionMessage,
                    l.CorrelationId, l.RequestId, l.TraceId, l.SpanId,
                    l.Category, l.EventId, l.EventName, l.Properties)).ToList();

                _logger.LogInformation("Filter logs query returned {Count} result(s) out of {Total}", logItems.Count, totalCount);

                return new FilterLogsResult(logItems, totalCount, request.Page, request.PageSize, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while filtering logs");
                return new FilterLogsResult(null, 0, request.Page, request.PageSize, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request. Please try again later."
                });
            }
        }
    }
}
