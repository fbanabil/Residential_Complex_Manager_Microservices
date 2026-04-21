using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Mapster;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.FilterParkingSlot
{
    public record FilterParkingSlotRequest(long? ParkingSpaceCode,long? AssignedUnitCode,long? SlotCode,string? SlotType, string? Status);

    public record FilterParkingSlotResponseInstance(Guid Id, long SlotCode, string SlotType,string Status,long ParkingSpaceCode,string ParkingSpaceName, long? AssignedUnitCode, string? AssignedUnitNo);

    public record FilterParkingSlotResponse(List<FilterParkingSlotResponseInstance>? ParkingSlots);

    public class FilterParkingSlotValidator : AbstractValidator<FilterParkingSlotRequest>
    {
        public FilterParkingSlotValidator()
        {
            RuleFor(x => x.ParkingSpaceCode)
                .GreaterThanOrEqualTo(3000000000)
                .When(x => x.ParkingSpaceCode.HasValue)
                .WithMessage("Parking space code must be greater than or equal to 3000000000.");

            RuleFor(x => x.ParkingSpaceCode)
                .LessThan(4000000000)
                .When(x => x.ParkingSpaceCode.HasValue)
                .WithMessage("Parking space code must be less than 4000000000.");

            RuleFor(x => x.AssignedUnitCode)
                .GreaterThanOrEqualTo(5000000000)
                .When(x => x.AssignedUnitCode.HasValue)
                .WithMessage("Assigned unit code must be greater than or equal to 5000000000.");

            RuleFor(x => x.AssignedUnitCode)
                .LessThan(6000000000)
                .When(x => x.AssignedUnitCode.HasValue)
                .WithMessage("Assigned unit code must be less than 6000000000.");

            RuleFor(x => x.SlotType)
                .IsEnumName(typeof(SlotType))
                .When(x => !string.IsNullOrWhiteSpace(x.SlotType))
                .WithMessage("Slot type must be one of: Compact, Standard, Large, Handicapped.");

            RuleFor(x => x.Status)
                .IsEnumName(typeof(Status))
                .When(x => !string.IsNullOrWhiteSpace(x.Status))
                .WithMessage("Status must be one of: Active, Inactive, Maintenance.");
        }
    }

    public class FilterParkingSlotEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/parking-slots/filter", async ([AsParameters] FilterParkingSlotRequest request, ISender sender, [FromServices] IValidator<FilterParkingSlotRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = request.Adapt<FilterParkingSlotQuery>();
                var result = await sender.Send(query);

                if (result.ParkingSlots == null || !result.ParkingSlots.Any())
                {
                    return Results.Ok(new FilterParkingSlotResponse(new List<FilterParkingSlotResponseInstance>()));
                }

                return Results.Ok(new FilterParkingSlotResponse(result.ParkingSlots));
            })
                .WithName("FilterParkingSlots")
                .WithTags("ParkingSlots")
                .Produces<FilterParkingSlotResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Filters parking slots based on provided criteria.");
        }
    }
}
