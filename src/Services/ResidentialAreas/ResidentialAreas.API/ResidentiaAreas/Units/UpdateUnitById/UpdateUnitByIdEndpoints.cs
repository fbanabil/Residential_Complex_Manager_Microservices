using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.Units.UpdateUnitById
{
    public record UpdateUnitByIdRequest(Guid Id, long BuildingCode, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages);

    public record UpdateUnitByIdResponse(Guid Id, long Code, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, DateTime CreatedAt, DateTime UpdatedAt, long BuildingCode, string BuildingName, List<string?>? ImageUrls);

    public class UpdateUnitByIdValidator : AbstractValidator<UpdateUnitByIdRequest>
    {
        public UpdateUnitByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Unit Id is required.");

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

            RuleFor(x => x.AddedBase64StringImages)
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringLiset(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }
    }

    public class UpdateUnitByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/units/update-by-id", async (HttpContext httpContext, UpdateUnitByIdRequest request, ISender sender, [FromServices] IValidator<UpdateUnitByIdRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<UpdateUnitByIdCommand>();
                var result = await sender.Send(command);

                if (result == null)
                {
                    return Results.NotFound("The unit with the specified ID was not found, or related building does not exist.");
                }

                var response = result.Adapt<UpdateUnitByIdResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("UpdateUnitById")
                .WithTags("Units")
                .Produces<UpdateUnitByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Updates a unit by its ID.");
        }
    }
}
