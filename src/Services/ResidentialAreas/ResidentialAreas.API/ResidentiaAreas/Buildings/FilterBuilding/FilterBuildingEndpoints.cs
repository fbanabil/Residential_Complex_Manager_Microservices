using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.FilterBuilding
{
    public record FilterBuildingRequest(long? AreaCode, string? Name, string? BlockNo, int? TotalFloors, string? Status);
    public record FilterBuildingResponseInstance(long Code, string Name, string BlockNo, int? TotalFloors, string Address, string Status, long AreaCode, string AreaName, List<string?>? ImageUrls);
    public record FilterBuildingResponse(List<FilterBuildingResponseInstance>? Buildings);

    public class FilterBuildingValidator : AbstractValidator<FilterBuildingRequest>
    {
        public FilterBuildingValidator()
        {
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).When(x => x.AreaCode.HasValue).WithMessage("Area code must be greater than or equal to 1000000000.");
            RuleFor(x => x.AreaCode).LessThan(2000000000).When(x => x.AreaCode.HasValue).WithMessage("Area code must be less than 2000000000.");
            RuleFor(x => x.Status).IsEnumName(typeof(Status)).When(x => !string.IsNullOrEmpty(x.Status)).WithMessage("The status must be a valid value (Active, Inactive, Maintenance).");
        }
    }

    public class FilterBuildingEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/buildings/filter", async (HttpContext httpContext, [AsParameters] FilterBuildingRequest request, ISender sender, [FromServices] IValidator<FilterBuildingRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = request.Adapt<FilterBuildingQuery>();
                var result = await sender.Send(query);

                if (result.Buildings == null || !result.Buildings.Any())
                {
                    return Results.Ok(new FilterBuildingResponse(new List<FilterBuildingResponseInstance>()));
                }

                var response = new FilterBuildingResponse(
                    result.Buildings.Select(building => building with
                    {
                        ImageUrls = building.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                    }).ToList());

                return Results.Ok(response);
            })
                .WithName("FilterBuildings")
                .WithTags("Buildings")
                .Produces<FilterBuildingResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Filters buildings based on the provided criteria as parameters.");
        }
    }
}