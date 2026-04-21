using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Units.GetUnitById
{
    public record GetUnitByIdResponse(Guid Id, long Code, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, DateTime CreatedAt, DateTime UpdatedAt, long BuildingCode, string BuildingName, List<string?>? ImageUrls);

    public class GetUnitByIdRequestValidator : AbstractValidator<Guid>
    {
        public GetUnitByIdRequestValidator()
        {
            RuleFor(id => id).NotEmpty().WithMessage("The unit ID is required.");
        }
    }

    public class GetUnitByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/units/{id:guid}", async (HttpContext httpContext, Guid id, ISender sender, [FromServices] IValidator<Guid> validator) =>
            {
                var validationResult = await validator.ValidateAsync(id);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.ToDictionary());
                }

                var query = new GetUnitByIdQuery(id);
                var result = await sender.Send(query);

                if (result == null)
                {
                    return Results.NotFound($"Unit with ID {id} not found.");
                }

                var response = result.Adapt<GetUnitByIdResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("GetUnitById")
                .WithTags("Units")
                .Produces<GetUnitByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a unit by its ID.");
        }
    }
}
