using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Mapster;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.AddNewParkingSlot
{
    public record AddNewParkingSlotRequest(long ParkingSpaceCode,long? AssignedUnitCode,string SlotType,string Status);

    public record AddNewParkingSlotResponse(Guid Id,long SlotCode,string SlotType,string Status,long ParkingSpaceCode,string ParkingSpaceName,long? AssignedUnitCode,string? AssignedUnitNo);

    public class AddNewParkingSlotValidator : AbstractValidator<AddNewParkingSlotRequest>
    {
        public AddNewParkingSlotValidator()
        {
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

    public class AddNewParkingSlotEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/parking-slots/add", async (AddNewParkingSlotRequest request, ISender sender, [FromServices] IValidator<AddNewParkingSlotRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<AddNewParkingSlotCommand>();
                var result = await sender.Send(command);

                if (result == null)
                {
                    return Results.Problem("Failed to add parking slot. Ensure parking space/unit exists and request is valid.");
                }

                var response = result.Adapt<AddNewParkingSlotResponse>();
                return Results.Created($"/parking-slots/{response.Id}", response);
            })
                .WithName("AddNewParkingSlot")
                .WithTags("ParkingSlots")
                .Produces<AddNewParkingSlotResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Adds a new parking slot under a parking space.");
        }
    }
}
