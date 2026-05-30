namespace QueryService.API.Apis.Logs.GetServiceNames
{
    public record GetServiceNamesResponse(List<string> ServiceNames, int Count);

    public class GetServiceNamesEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/logs/services", async (ISender sender, ILogger<GetServiceNamesEndpoints> logger) =>
            {
                var query = new GetServiceNamesQuery();
                var result = await sender.Send(query);

                if (result.Error is not null)
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);

                var names = result.ServiceNames ?? new List<string>();
                return Results.Ok(new GetServiceNamesResponse(names, names.Count));
            })
                .WithName("GetServiceNames")
                .WithTags("Logs")
                .Produces<GetServiceNamesResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithSummary("Returns the distinct list of service names that have produced logs.")
                .RequireAuthorization("AdminOnly");
        }
    }
}
