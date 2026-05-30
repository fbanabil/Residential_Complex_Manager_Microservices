using QueryService.API.Apis.Logs.FilterLogs;
using QueryService.API.Repository;

namespace QueryService.API.Apis.Logs.GetLogsByCorrelationId
{
    public record GetLogsByCorrelationIdQuery(string CorrelationId) : IQuery<GetLogsByCorrelationIdResult>;

    public record GetLogsByCorrelationIdResult(List<LogItemResponse>? Logs, ErrorCarrier? Error);

    public class GetLogsByCorrelationIdHandler : IQueryHandler<GetLogsByCorrelationIdQuery, GetLogsByCorrelationIdResult>
    {
        private readonly LogQueryRepository _repository;
        private readonly ILogger<GetLogsByCorrelationIdHandler> _logger;

        public GetLogsByCorrelationIdHandler(LogQueryRepository repository, ILogger<GetLogsByCorrelationIdHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GetLogsByCorrelationIdResult> Handle(GetLogsByCorrelationIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var logs = await _repository.GetLogsByCorrelationIdAsync(request.CorrelationId, cancellationToken);

                if (logs.Count == 0)
                {
                    return new GetLogsByCorrelationIdResult(null, new ErrorCarrier
                    {
                        Title = "NOT_FOUND",
                        StatusCode = StatusCodes.Status404NotFound,
                        Detail = $"No logs found for correlation id '{request.CorrelationId}'."
                    });
                }

                var logItems = logs.Select(l => new LogItemResponse(
                    l.Id, l.ServiceName, l.Environment, l.LogLevel, l.Message,
                    l.Timestamp, l.ExceptionType, l.ExceptionMessage,
                    l.CorrelationId, l.RequestId, l.TraceId, l.SpanId,
                    l.Category, l.EventId, l.EventName, l.Properties)).ToList();

                _logger.LogInformation("GetLogsByCorrelationId returned {Count} logs for correlationId: {CorrelationId}", logItems.Count, request.CorrelationId);

                return new GetLogsByCorrelationIdResult(logItems, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting logs by correlation id: {CorrelationId}", request.CorrelationId);
                return new GetLogsByCorrelationIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request. Please try again later."
                });
            }
        }
    }
}
