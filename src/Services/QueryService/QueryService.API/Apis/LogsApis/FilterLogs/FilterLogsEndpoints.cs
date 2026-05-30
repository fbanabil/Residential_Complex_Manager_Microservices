namespace QueryService.API.Apis.Logs.FilterLogs
{
    public record FilterLogsRequest(
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
        int? Page,
        int? PageSize,
        string? SortBy,
        string? SortOrder);

    public record FilterLogsResponse(List<LogItemResponse>? Logs, long TotalCount, int Page, int PageSize, int TotalPages);

    public class FilterLogsRequestValidator : AbstractValidator<FilterLogsRequest>
    {
        private static readonly HashSet<string> ValidLogLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            "Trace", "Debug", "Information", "Warning", "Error", "Critical"
        };

        public FilterLogsRequestValidator()
        {
            RuleFor(x => x.LogLevel)
                .Must(level => ValidLogLevels.Contains(level!))
                .When(x => !string.IsNullOrEmpty(x.LogLevel))
                .WithMessage("LogLevel must be one of: Trace, Debug, Information, Warning, Error, Critical.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .When(x => x.Page.HasValue)
                .WithMessage("Page must be greater than or equal to 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .When(x => x.PageSize.HasValue)
                .WithMessage("PageSize must be between 1 and 100.");

            RuleFor(x => x.SortBy)
                .Must(s => s is "timestamp" or "loglevel" or "servicename")
                .When(x => !string.IsNullOrEmpty(x.SortBy))
                .WithMessage("SortBy must be one of: timestamp, loglevel, servicename.");

            RuleFor(x => x.SortOrder)
                .Must(s => s is "asc" or "desc")
                .When(x => !string.IsNullOrEmpty(x.SortOrder))
                .WithMessage("SortOrder must be 'asc' or 'desc'.");

            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be earlier than or equal to ToDate.");
        }
    }

    public class FilterLogsEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/logs/filter", async ([AsParameters] FilterLogsRequest request, ISender sender, IValidator<FilterLogsRequest> validator, ILogger<FilterLogsEndpoints> logger) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    logger.LogWarning("Filter logs failed: validation error");
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = new FilterLogsQuery(
                    request.ServiceName,
                    request.LogLevel,
                    request.Message,
                    request.CorrelationId,
                    request.RequestId,
                    request.TraceId,
                    request.Category,
                    request.ExceptionType,
                    request.FromDate,
                    request.ToDate,
                    request.Page ?? 1,
                    request.PageSize ?? 20,
                    request.SortBy,
                    request.SortOrder ?? "desc");

                var result = await sender.Send(query);

                if (result.Error is not null)
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);

                var totalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize);

                return Results.Ok(new FilterLogsResponse(result.Logs, result.TotalCount, result.Page, result.PageSize, totalPages));
            })
                .WithName("FilterLogs")
                .WithTags("Logs")
                .Produces<FilterLogsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Filters and queries logs with pagination, sorting, and various filter criteria.")
                .RequireAuthorization("AdminOnly");
        }
    }
}
