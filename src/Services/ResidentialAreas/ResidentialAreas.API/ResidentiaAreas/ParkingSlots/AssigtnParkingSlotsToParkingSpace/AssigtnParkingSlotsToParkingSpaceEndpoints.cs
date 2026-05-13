using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.AssigtnParkingSlotsToParkingSpace
{
    public record AssigtnParkingSlotsToParkingSpaceRequest(long ParkingSpaceCode, List<long> SlotCodes);
    public record AssigtnParkingSlotsToParkingSpaceResponse(bool Success, string Message);

    public class AssigtnParkingSlotsToParkingSpaceValidator : AbstractValidator<AssigtnParkingSlotsToParkingSpaceRequest>
    {
        public AssigtnParkingSlotsToParkingSpaceValidator()
        {
            RuleFor(x => x.ParkingSpaceCode).GreaterThanOrEqualTo(3000000000).WithMessage("Parking space code must be greater than or equal to 3000000000.");
            RuleFor(x => x.ParkingSpaceCode).LessThan(4000000000).WithMessage("Parking space code must be less than 4000000000.");

            RuleFor(x => x.SlotCodes)
                .NotEmpty().WithMessage("At least one slot code is required.");

            RuleForEach(x => x.SlotCodes)
                .GreaterThan(0).WithMessage("Slot code must be a positive number.");
        }
    }

    public class AssigtnParkingSlotsToParkingSpaceEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/parking-slots/assign-to-parking-space", HandleAssignParkingSlotsToParkingSpace)
                .WithName("AssignParkingSlotsToParkingSpace")
                .WithTags("ParkingSlots")
                .WithSummary("Assigns parking slots to a parking space.")
                .Produces<AssigtnParkingSlotsToParkingSpaceResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireAuthorization("AdminOrComplexManager");
        }

        private static async Task<IResult> HandleAssignParkingSlotsToParkingSpace(AssigtnParkingSlotsToParkingSpaceRequest request, ISender sender, [FromServices] IValidator<AssigtnParkingSlotsToParkingSpaceRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var command = request.Adapt<AssigtnParkingSlotsToParkingSpaceCommand>();
            var result = await sender.Send(command);

            if (result.Error != null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }

            var response = result.Result?.Adapt<AssigtnParkingSlotsToParkingSpaceResponse>();
            return Results.Ok(response);
        }
    }
}