using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.GetParkingSlotById
{
    public record GetParkingSlotByIdResponse(Guid Id,long SlotCode, string SlotType, string Status, DateTime CreatedAt,DateTime UpdatedAt,long ParkingSpaceCode, string ParkingSpaceName,long? AssignedUnitCode, string? AssignedUnitNo);

    public class GetParkingSlotByIdRequestValidator : AbstractValidator<Guid>
    {
        public GetParkingSlotByIdRequestValidator()
        {
            RuleFor(id => id).NotEmpty().WithMessage("The parking slot ID is required.");
        }
    }

    public class GetParkingSlotByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/parking-slots/{id:guid}", async (Guid id, ISender sender, [FromServices] IValidator<Guid> validator) =>
            {
                var validationResult = await validator.ValidateAsync(id);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.ToDictionary());
                }

                var query = new GetParkingSlotByIdQuery(id);
                var result = await sender.Send(query);

                if (result == null)
                {
                    return Results.NotFound($"Parking slot with ID {id} not found.");
                }

                var response = result.Adapt<GetParkingSlotByIdResponse>();
                return Results.Ok(response);
            })
                .WithName("GetParkingSlotById")
                .WithTags("ParkingSlots")
                .Produces<GetParkingSlotByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a parking slot by its ID.");
        }
    }
}
