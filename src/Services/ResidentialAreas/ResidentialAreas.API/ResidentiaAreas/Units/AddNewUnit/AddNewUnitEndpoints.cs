using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.Units.AddNewUnit
{
    public record AddNewUnitRequest(long BuildingCode, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, List<string?>? ImageBase64);

    public record AddNewUnitResponse(Guid Id, long Code, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, long BuildingCode, string BuildingName, List<string?>? ImageUrls);

    public class AddNewUnitValidator : AbstractValidator<AddNewUnitRequest>
    {
        public AddNewUnitValidator()
        {
            RuleFor(x => x.BuildingCode)
                .GreaterThanOrEqualTo(2000000000).WithMessage("Building code must be greater than or equal to 2000000000.");
            RuleFor(x => x.BuildingCode)
                .LessThan(3000000000).WithMessage("Building code must be less than 3000000000.");

            RuleFor(x => x.UnitNo)
                .NotEmpty().WithMessage("Unit number is required.")
                .MaximumLength(20).WithMessage("Unit number cannot exceed 20 characters.");

            RuleFor(x => x.FloorNo)
                .GreaterThanOrEqualTo(0).WithMessage("Floor number must be greater than or equal to 0.");

            RuleFor(x => x.UnitType)
                .NotEmpty().WithMessage("Unit type is required.")
                .IsEnumName(typeof(UnitType)).WithMessage("Unit type must be one of: Apartment, Shop, Office, Parking, Storage.");

            RuleFor(x => x.Bedrooms)
                .GreaterThanOrEqualTo(0).When(x => x.Bedrooms.HasValue)
                .WithMessage("Bedrooms must be greater than or equal to 0.");

            RuleFor(x => x.Bathrooms)
                .GreaterThanOrEqualTo(0).When(x => x.Bathrooms.HasValue)
                .WithMessage("Bathrooms must be greater than or equal to 0.");

            RuleFor(x => x.AreaSqft)
                .GreaterThan(0).WithMessage("AreaSqft must be greater than 0.");

            RuleFor(x => x.OccupancyStatus)
                .NotEmpty().WithMessage("Occupancy status is required.")
                .IsEnumName(typeof(OccupancyStatus)).WithMessage("Occupancy status must be one of: Vacant, Occupied, Reserved, Maintenance.");

            RuleFor(x => x.OwnershipType)
                .NotEmpty().WithMessage("Ownership type is required.")
                .IsEnumName(typeof(OwnershipType)).WithMessage("Ownership type must be one of: Owned, Rented, Association.");

            RuleFor(x => x.ImageBase64)
                .NotEmpty().WithMessage("At least one image is required.")
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringLiset(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }
    }

    public class AddNewUnitEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/units/add", async (HttpContext httpContext, AddNewUnitRequest request, ISender sender, [FromServices] IValidator<AddNewUnitRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<AddNewUnitCommand>();
                var result = await sender.Send(command);

                if (result == null)
                {
                    return Results.Problem("Failed to add unit. Ensure the building exists and the request is valid.");
                }

                var response = result.Adapt<AddNewUnitResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Created($"/units/{response.Id}", response);
            })
                .WithName("AddNewUnit")
                .WithTags("Units")
                .Produces<AddNewUnitResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Adds a new unit under a building.");
        }
    }
}
