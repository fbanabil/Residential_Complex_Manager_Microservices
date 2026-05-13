using ResidentialAreas.API.Helpers.ErrorCarrier;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.AssigtnParkingSlotsToParkingSpace
{
    public record AssigtnParkingSlotsToParkingSpaceCommand(long ParkingSpaceCode, List<long> SlotCodes) : ICommand<AssigtnParkingSlotsToParkingSpaceResult>;
    public record AssigtnParkingSlotsToParkingSpaceResult(AssigtnParkingSlotsToParkingSpaceResponse? Result, ErrorCarrier? Error);

    public class AssigtnParkingSlotsToParkingSpaceHandler : ICommandHandler<AssigtnParkingSlotsToParkingSpaceCommand, AssigtnParkingSlotsToParkingSpaceResult>
    {
        private readonly AreaDbContext _areaDbContext;

        public AssigtnParkingSlotsToParkingSpaceHandler(AreaDbContext areaDbContext)
        {
            _areaDbContext = areaDbContext;
        }

        public async Task<AssigtnParkingSlotsToParkingSpaceResult> Handle(AssigtnParkingSlotsToParkingSpaceCommand request, CancellationToken cancellationToken)
        {
            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking().FirstOrDefaultAsync(p => p.ParkingSpaceCode == request.ParkingSpaceCode, cancellationToken);
            if (parkingSpace == null)
            {
                return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "PARKING_SPACE_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No parking space found with code {request.ParkingSpaceCode}."
                });
            }



            List<long> slotCodes = request.SlotCodes.Distinct().ToList();
            List<ParkingSlot> slots = await _areaDbContext.ParkingSlots.AsNoTracking().Where(s => slotCodes.Contains(s.SlotCode)).ToListAsync(cancellationToken);
            if (slots.Count != slotCodes.Count)
            {
                List<long> missingCodes = slotCodes.Except(slots.Select(s => s.SlotCode)).ToList();
                return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "PARKING_SLOTS_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"Missing parking slot codes: {string.Join(", ", missingCodes)}."
                });
            }




            try
            {
                int updated = await _areaDbContext.ParkingSlots.Where(s => slotCodes.Contains(s.SlotCode)).ExecuteUpdateAsync(setters => setters.SetProperty(s => s.ParkingSpaceId, parkingSpace.Id).SetProperty(s => s.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (updated == 0)
                {
                    return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign parking slots to the parking space."
                    });
                }
            }
            catch
            {
                return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "DATABASE_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning parking slots."
                });
            }



            return new AssigtnParkingSlotsToParkingSpaceResult(new AssigtnParkingSlotsToParkingSpaceResponse(true, $"{slotCodes.Count} parking slot(s) assigned to parking space {request.ParkingSpaceCode}."), null);
        }
    }
}