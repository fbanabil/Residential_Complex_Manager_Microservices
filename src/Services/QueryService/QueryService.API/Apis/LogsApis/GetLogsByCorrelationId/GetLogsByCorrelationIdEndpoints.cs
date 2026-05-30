using QueryService.API.Apis.Logs.FilterLogs;

namespace QueryService.API.Apis.Logs.GetLogsByCorrelationId
{
    public record GetLogsByCorrelationIdResponse(List<LogItemResponse> Logs);

    public class GetLogsByCorrelationIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/logs/correlation/{correlationId}", async (string correlationId, ISender sender, ILogger<GetLogsByCorrelationIdEndpoints> logger) =>
            {
                var query = new GetLogsByCorrelationIdQuery(correlationId);
                var result = await sender.Send(query);

                if (result.Error is not null)
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);

                return Results.Ok(new GetLogsByCorrelationIdResponse(result.Logs!));
            })
                .WithName("GetLogsByCorrelationId")
                .WithTags("Logs")
                .Produces<GetLogsByCorrelationIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Gets all log entries associated with a specific correlation ID for tracing a request across services.")
                .RequireAuthorization("AdminOnly");
        }
    }
}
