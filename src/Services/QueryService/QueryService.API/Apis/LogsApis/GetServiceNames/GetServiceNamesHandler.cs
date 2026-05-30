using QueryService.API.Repository;

namespace QueryService.API.Apis.Logs.GetServiceNames
{
    public record GetServiceNamesQuery() : IQuery<GetServiceNamesResult>;

    public record GetServiceNamesResult(List<string>? ServiceNames, ErrorCarrier? Error);

    public class GetServiceNamesHandler : IQueryHandler<GetServiceNamesQuery, GetServiceNamesResult>
    {
        private readonly LogQueryRepository _repository;
        private readonly ILogger<GetServiceNamesHandler> _logger;

        public GetServiceNamesHandler(LogQueryRepository repository, ILogger<GetServiceNamesHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GetServiceNamesResult> Handle(GetServiceNamesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var serviceNames = await _repository.GetDistinctServiceNamesAsync(cancellationToken);

                var ordered = serviceNames
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _logger.LogInformation("GetServiceNames returned {Count} distinct service(s)", ordered.Count);

                return new GetServiceNamesResult(ordered, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting distinct service names");
                return new GetServiceNamesResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request. Please try again later."
                });
            }
        }
    }
}
