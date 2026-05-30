namespace QueryService.API.Apis.Logs.TimeSeries
{
    public record TimeSeriesRequest(
        string? Bucket,
        string? ServiceName,
        string? LogLevel,
        DateTime? FromDate,
        DateTime? ToDate);

    public record TimeSeriesResponse(string Bucket, List<TimeSeriesPoint> Points);

    public class TimeSeriesRequestValidator : AbstractValidator<TimeSeriesRequest>
    {
        private static readonly HashSet<string> ValidBuckets = new(StringComparer.OrdinalIgnoreCase)
        {
            "minute", "hour", "day", "week", "month"
        };

        private static readonly HashSet<string> ValidLogLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            "Trace", "Debug", "Information", "Warning", "Error", "Critical"
        };

        public TimeSeriesRequestValidator()
        {
            RuleFor(x => x.Bucket)
                .Must(b => ValidBuckets.Contains(b!))
                .When(x => !string.IsNullOrEmpty(x.Bucket))
                .WithMessage("Bucket must be one of: minute, hour, day, week, month.");

            RuleFor(x => x.LogLevel)
                .Must(level => ValidLogLevels.Contains(level!))
                .When(x => !string.IsNullOrEmpty(x.LogLevel))
                .WithMessage("LogLevel must be one of: Trace, Debug, Information, Warning, Error, Critical.");

            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be earlier than or equal to ToDate.");
        }
    }

    public class TimeSeriesEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/logs/stats/timeseries", async ([AsParameters] TimeSeriesRequest request, ISender sender, IValidator<TimeSeriesRequest> validator, ILogger<TimeSeriesEndpoints> logger) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    logger.LogWarning("TimeSeries failed: validation error");
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var bucket = (request.Bucket ?? "hour").ToLowerInvariant();

                var query = new TimeSeriesQuery(bucket, request.ServiceName, request.LogLevel, request.FromDate, request.ToDate);
                var result = await sender.Send(query);

                if (result.Error is not null)
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);

                return Results.Ok(new TimeSeriesResponse(bucket, result.Points ?? new List<TimeSeriesPoint>()));
            })
                .WithName("LogsTimeSeries")
                .WithTags("Logs.Stats")
                .Produces<TimeSeriesResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Returns log counts bucketed over time (minute/hour/day/week/month), broken down by LogLevel.")
                .RequireAuthorization("AdminOnly");
        }
    }
}
