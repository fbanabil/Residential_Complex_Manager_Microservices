using QueryService.API.Apis.Logs.FilterLogs;

namespace QueryService.API.Apis.Logs.GetLogsByTraceId
{
    public record GetLogsByTraceIdResponse(List<LogItemResponse> Logs);

    public class GetLogsByTraceIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/logs/trace/{traceId}", async (string traceId, ISender sender, ILogger<GetLogsByTraceIdEndpoints> logger) =>
            {
                var query = new GetLogsByTraceIdQuery(traceId);
                var result = await sender.Send(query);

                if (result.Error is not null)
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);

                return Results.Ok(new GetLogsByTraceIdResponse(result.Logs!));
            })
                .WithName("GetLogsByTraceId")
                .WithTags("Logs")
                .Produces<GetLogsByTraceIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Gets all log entries associated with a specific W3C trace ID for distributed tracing across services.")
                .RequireAuthorization("AdminOnly");
        }
    }
}
