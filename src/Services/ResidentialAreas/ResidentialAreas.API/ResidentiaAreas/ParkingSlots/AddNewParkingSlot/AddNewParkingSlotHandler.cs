namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.AddNewParkingSlot
{
    public record AddNewParkingSlotCommand(long ParkingSpaceCode, long? AssignedUnitCode,string SlotType,string Status) : ICommand<AddNewParkingSlotResult>;

    public record AddNewParkingSlotResult( Guid Id,long SlotCode,string SlotType,string Status,long ParkingSpaceCode,string ParkingSpaceName, long? AssignedUnitCode,string? AssignedUnitNo);

    public class AddNewParkingSlotHandler : ICommandHandler<AddNewParkingSlotCommand, AddNewParkingSlotResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewParkingSlotHandler> _logger;

        public AddNewParkingSlotHandler(AreaDbContext areaDbContext, ILogger<AddNewParkingSlotHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<AddNewParkingSlotResult> Handle(AddNewParkingSlotCommand request, CancellationToken cancellationToken)
        {
            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParkingSpaceCode == request.ParkingSpaceCode, cancellationToken);

            if (parkingSpace == null)
            {
                _logger.LogWarning("Parking space with code {ParkingSpaceCode} not found while creating parking slot.", request.ParkingSpaceCode);
                return null;
            }

            EntityModels.Unit? unit = null;
            if (request.AssignedUnitCode.HasValue)
            {
                unit = await _areaDbContext.Units.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Code == request.AssignedUnitCode.Value, cancellationToken);

                if (unit == null)
                {
                    _logger.LogWarning("Assigned unit with code {UnitCode} not found while creating parking slot.", request.AssignedUnitCode.Value);
                    return null;
                }
            }

            ParkingSlot parkingSlot = new ParkingSlot
            {
                Id = Guid.NewGuid(),
                ParkingSpaceId = parkingSpace.Id,
                AssignedUnitId = unit?.Id,
                SlotType = System.Enum.Parse<SlotType>(request.SlotType, true),
                Status = System.Enum.Parse<Status>(request.Status, true),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _areaDbContext.ParkingSlots.AddAsync(parkingSlot, cancellationToken);
            await _areaDbContext.SaveChangesAsync(cancellationToken);

            return new AddNewParkingSlotResult(
                parkingSlot.Id ?? Guid.Empty,
                parkingSlot.SlotCode,
                parkingSlot.SlotType.ToString(),
                parkingSlot.Status.ToString(),
                parkingSpace.ParkingSpaceCode,
                parkingSpace.Name ?? string.Empty,
                unit?.Code,
                unit?.UnitNo);
        }
    }
}
