using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.GetBuildingById
{
    public record GetBuildingByIdResponse(Guid Id, long Code, string Name, string BlockNo, int? TotalFloors, string Address, string Status, DateTime CreatedAt, DateTime UpdatedAt, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class GetBuildingByIdRequestValidator : AbstractValidator<Guid>
    {
        public GetBuildingByIdRequestValidator()
        {
            RuleFor(id => id).NotEmpty().WithMessage("The building ID is required.");
        }
    }

    public class GetBuildingByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/buildings/{id:guid}", async (HttpContext httpContext, Guid id, ISender sender, [FromServices] IValidator<Guid> validator) =>
            {
                var validationResult = await validator.ValidateAsync(id);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.ToDictionary());
                }

                var query = new GetBuildingByIdQuery(id);
                var result = await sender.Send(query);

                if (result == null)
                {
                    return Results.NotFound($"Building with ID {id} not found.");
                }

                var response = result.Adapt<GetBuildingByIdResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("GetBuildingById")
                .WithTags("Buildings")
                .Produces<GetBuildingByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a building by its ID.");
        }
    }
}
