using QueryService.API.Apis.Logs.FilterLogs;
using QueryService.API.Repository;

namespace QueryService.API.Apis.Logs.GetLogById
{
    public record GetLogByIdQuery(string Id) : IQuery<GetLogByIdResult>;

    public record GetLogByIdResult(LogItemResponse? Log, ErrorCarrier? Error);

    public class GetLogByIdHandler : IQueryHandler<GetLogByIdQuery, GetLogByIdResult>
    {
        private readonly LogQueryRepository _repository;
        private readonly ILogger<GetLogByIdHandler> _logger;

        public GetLogByIdHandler(LogQueryRepository repository, ILogger<GetLogByIdHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GetLogByIdResult> Handle(GetLogByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var log = await _repository.GetLogByIdAsync(request.Id, cancellationToken);

                if (log is null)
                {
                    return new GetLogByIdResult(null, new ErrorCarrier
                    {
                        Title = "NOT_FOUND",
                        StatusCode = StatusCodes.Status404NotFound,
                        Detail = $"Log with id '{request.Id}' not found."
                    });
                }

                var response = new LogItemResponse(
                    log.Id, log.ServiceName, log.Environment, log.LogLevel, log.Message,
                    log.Timestamp, log.ExceptionType, log.ExceptionMessage,
                    log.CorrelationId, log.RequestId, log.TraceId, log.SpanId,
                    log.Category, log.EventId, log.EventName, log.Properties);

                return new GetLogByIdResult(response, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting log by id: {Id}", request.Id);
                return new GetLogByIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request. Please try again later."
                });
            }
        }
    }
}
