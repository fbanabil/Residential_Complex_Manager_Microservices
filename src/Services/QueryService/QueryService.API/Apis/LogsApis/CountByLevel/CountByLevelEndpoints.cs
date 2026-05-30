namespace QueryService.API.Apis.Logs.CountByLevel
{
    public record CountByLevelRequest(
        string? ServiceName,
        DateTime? FromDate,
        DateTime? ToDate);

    public record CountByLevelResponse(List<LevelCountItem> Items, long TotalCount);

    public class CountByLevelRequestValidator : AbstractValidator<CountByLevelRequest>
    {
        public CountByLevelRequestValidator()
        {
            RuleFor(x => x)
                .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
                .WithMessage("FromDate must be earlier than or equal to ToDate.");
        }
    }

    public class CountByLevelEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/logs/stats/by-level", async ([AsParameters] CountByLevelRequest request, ISender sender, IValidator<CountByLevelRequest> validator, ILogger<CountByLevelEndpoints> logger) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    logger.LogWarning("CountByLevel failed: validation error");
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = new CountByLevelQuery(request.ServiceName, request.FromDate, request.ToDate);
                var result = await sender.Send(query);

                if (result.Error is not null)
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);

                return Results.Ok(new CountByLevelResponse(result.Items ?? new List<LevelCountItem>(), result.TotalCount));
            })
                .WithName("CountLogsByLevel")
                .WithTags("Logs.Stats")
                .Produces<CountByLevelResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Returns the count of logs grouped by LogLevel, optionally filtered by service and date range.")
                .RequireAuthorization("AdminOnly");
        }
    }
}
