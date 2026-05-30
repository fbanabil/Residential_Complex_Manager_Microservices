namespace QueryService.API.Apis.Logs.CountByService
{
    public record CountByServiceRequest(
        string? LogLevel,
        DateTime? FromDate,
        DateTime? ToDate);

    public record CountByServiceResponse(List<ServiceCountItem> Items, long TotalCount);

    public class CountByServiceRequestValidator : AbstractValidator<CountByServiceRequest>
    {
        private static readonly HashSet<string> ValidLogLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            "Trace", "Debug", "Information", "Warning", "Error", "Critical"
        };

        public CountByServiceRequestValidator()
        {
            RuleFor(x => x.LogLevel)
                .Must(level => ValidLogLevels.Contains(level!))
                .When(x => !string.IsNullOrEmpty(x.LogLevel))
                .WithMessage("LogLevel must be one of: Trace, Debug, Information, Warning, Error, Critical.");

            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be earlier than or equal to ToDate.");
        }
    }

    public class CountByServiceEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/logs/stats/by-service", async ([AsParameters] CountByServiceRequest request, ISender sender, IValidator<CountByServiceRequest> validator, ILogger<CountByServiceEndpoints> logger) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    logger.LogWarning("CountByService failed: validation error");
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = new CountByServiceQuery(request.LogLevel, request.FromDate, request.ToDate);
                var result = await sender.Send(query);

                if (result.Error is not null)
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);

                return Results.Ok(new CountByServiceResponse(result.Items ?? new List<ServiceCountItem>(), result.TotalCount));
            })
                .WithName("CountLogsByService")
                .WithTags("Logs.Stats")
                .Produces<CountByServiceResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Returns the count of logs grouped by ServiceName, optionally filtered by log level and date range.")
                .RequireAuthorization("AdminOnly");
        }
    }
}
