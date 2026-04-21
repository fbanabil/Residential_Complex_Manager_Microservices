using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.UpdateParkingSlotById
{
    public record UpdateParkingSlotByIdRequest(Guid Id, long ParkingSpaceCode,long? AssignedUnitCode,string SlotType, string Status);

    public record UpdateParkingSlotByIdResponse(Guid Id,long SlotCode,string SlotType, string Status, DateTime CreatedAt, DateTime UpdatedAt,long ParkingSpaceCode, string ParkingSpaceName,long? AssignedUnitCode, string? AssignedUnitNo);

    public class UpdateParkingSlotByIdValidator : AbstractValidator<UpdateParkingSlotByIdRequest>
    {
        public UpdateParkingSlotByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Parking slot Id is required.");

            RuleFor(x => x.ParkingSpaceCode)
                .GreaterThanOrEqualTo(3000000000).WithMessage("Parking space code must be greater than or equal to 3000000000.");
            RuleFor(x => x.ParkingSpaceCode)
                .LessThan(4000000000).WithMessage("Parking space code must be less than 4000000000.");

            RuleFor(x => x.AssignedUnitCode)
                .GreaterThanOrEqualTo(5000000000)
                .When(x => x.AssignedUnitCode.HasValue)
                .WithMessage("Assigned unit code must be greater than or equal to 5000000000.");

            RuleFor(x => x.AssignedUnitCode)
                .LessThan(6000000000)
                .When(x => x.AssignedUnitCode.HasValue)
                .WithMessage("Assigned unit code must be less than 6000000000.");

            RuleFor(x => x.SlotType)
                .NotEmpty().WithMessage("Slot type is required.")
                .IsEnumName(typeof(SlotType)).WithMessage("Slot type must be one of: Compact, Standard, Large, Handicapped.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .IsEnumName(typeof(Status)).WithMessage("Status must be one of: Active, Inactive, Maintenance.");
        }
    }

    public class UpdateParkingSlotByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/parking-slots/update-by-id", async (UpdateParkingSlotByIdRequest request, ISender sender, [FromServices] IValidator<UpdateParkingSlotByIdRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<UpdateParkingSlotByIdCommand>();
                var result = await sender.Send(command);

                if (result == null)
                {
                    return Results.NotFound("The parking slot with the specified ID was not found, or related parking space/unit does not exist.");
                }

                var response = result.Adapt<UpdateParkingSlotByIdResponse>();
                return Results.Ok(response);
            })
                .WithName("UpdateParkingSlotById")
                .WithTags("ParkingSlots")
                .Produces<UpdateParkingSlotByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Updates a parking slot by its ID.");
        }
    }
}
