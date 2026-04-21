using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Units.FilterUnit
{
    public record FilterUnitRequest(long? BuildingCode, string? UnitNo, int? FloorNo, string? UnitType, string? OccupancyStatus, string? OwnershipType);

    public record FilterUnitResponseInstance(Guid Id, long Code, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, long BuildingCode, string BuildingName, List<string?>? ImageUrls);

    public record FilterUnitResponse(List<FilterUnitResponseInstance>? Units);

    public class FilterUnitValidator : AbstractValidator<FilterUnitRequest>
    {
        public FilterUnitValidator()
        {
            RuleFor(x => x.BuildingCode)
                .GreaterThanOrEqualTo(2000000000)
                .When(x => x.BuildingCode.HasValue)
                .WithMessage("Building code must be greater than or equal to 2000000000.");

            RuleFor(x => x.BuildingCode)
                .LessThan(3000000000)
                .When(x => x.BuildingCode.HasValue)
                .WithMessage("Building code must be less than 3000000000.");

            RuleFor(x => x.UnitType)
                .IsEnumName(typeof(UnitType))
                .When(x => !string.IsNullOrWhiteSpace(x.UnitType))
                .WithMessage("Unit type must be one of: Apartment, Shop, Office, Parking, Storage.");

            RuleFor(x => x.OccupancyStatus)
                .IsEnumName(typeof(OccupancyStatus))
                .When(x => !string.IsNullOrWhiteSpace(x.OccupancyStatus))
                .WithMessage("Occupancy status must be one of: Vacant, Occupied, Reserved, Maintenance.");

            RuleFor(x => x.OwnershipType)
                .IsEnumName(typeof(OwnershipType))
                .When(x => !string.IsNullOrWhiteSpace(x.OwnershipType))
                .WithMessage("Ownership type must be one of: Owned, Rented, Association.");
        }
    }

    public class FilterUnitEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/units/filter", async (HttpContext httpContext, [AsParameters] FilterUnitRequest request, ISender sender, [FromServices] IValidator<FilterUnitRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = request.Adapt<FilterUnitQuery>();
                var result = await sender.Send(query);

                if (result.Units == null || !result.Units.Any())
                {
                    return Results.Ok(new FilterUnitResponse(new List<FilterUnitResponseInstance>()));
                }

                var response = new FilterUnitResponse(result.Units.Select(unit => unit with
                {
                    ImageUrls = unit.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                }).ToList());

                return Results.Ok(response);
            })
                .WithName("FilterUnits")
                .WithTags("Units")
                .Produces<FilterUnitResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Filters units based on provided criteria.");
        }
    }
}
