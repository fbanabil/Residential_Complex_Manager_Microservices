using QueryService.API.Apis.Logs.FilterLogs;
using QueryService.API.Repository;

namespace QueryService.API.Apis.Logs.GetLogsByTraceId
{
    public record GetLogsByTraceIdQuery(string TraceId) : IQuery<GetLogsByTraceIdResult>;

    public record GetLogsByTraceIdResult(List<LogItemResponse>? Logs, ErrorCarrier? Error);

    public class GetLogsByTraceIdHandler : IQueryHandler<GetLogsByTraceIdQuery, GetLogsByTraceIdResult>
    {
        private readonly LogQueryRepository _repository;
        private readonly ILogger<GetLogsByTraceIdHandler> _logger;

        public GetLogsByTraceIdHandler(LogQueryRepository repository, ILogger<GetLogsByTraceIdHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GetLogsByTraceIdResult> Handle(GetLogsByTraceIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var logs = await _repository.GetLogsByTraceIdAsync(request.TraceId, cancellationToken);

                if (logs.Count == 0)
                {
                    return new GetLogsByTraceIdResult(null, new ErrorCarrier
                    {
                        Title = "NOT_FOUND",
                        StatusCode = StatusCodes.Status404NotFound,
                        Detail = $"No logs found for trace id '{request.TraceId}'."
                    });
                }

                var logItems = logs.Select(l => new LogItemResponse(
                    l.Id, l.ServiceName, l.Environment, l.LogLevel, l.Message,
                    l.Timestamp, l.ExceptionType, l.ExceptionMessage,
                    l.CorrelationId, l.RequestId, l.TraceId, l.SpanId,
                    l.Category, l.EventId, l.EventName, l.Properties)).ToList();

                _logger.LogInformation("GetLogsByTraceId returned {Count} logs for traceId: {TraceId}", logItems.Count, request.TraceId);

                return new GetLogsByTraceIdResult(logItems, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting logs by trace id: {TraceId}", request.TraceId);
                return new GetLogsByTraceIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request. Please try again later."
                });
            }
        }
    }
}
