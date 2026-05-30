using QueryService.API.Apis.Logs.FilterLogs;

namespace QueryService.API.Apis.Logs.GetLogById
{
    public record GetLogByIdResponse(LogItemResponse Log);

    public class GetLogByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/logs/{id}", async (string id, ISender sender, ILogger<GetLogByIdEndpoints> logger) =>
            {
                var query = new GetLogByIdQuery(id);
                var result = await sender.Send(query);

                if (result.Error is not null)
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);

                return Results.Ok(new GetLogByIdResponse(result.Log!));
            })
                .WithName("GetLogById")
                .WithTags("Logs")
                .Produces<GetLogByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Gets a specific log entry by its ID.")
                .RequireAuthorization("AdminOnly");
        }
    }
}
