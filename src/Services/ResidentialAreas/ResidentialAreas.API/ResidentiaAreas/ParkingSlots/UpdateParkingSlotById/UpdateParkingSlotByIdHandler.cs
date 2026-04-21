namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.UpdateParkingSlotById
{
    public record UpdateParkingSlotByIdCommand(Guid Id,long ParkingSpaceCode, long? AssignedUnitCode, string SlotType,string Status) : ICommand<UpdateParkingSlotByIdResult>;

    public record UpdateParkingSlotByIdResult( Guid Id, long SlotCode, string SlotType,string Status, DateTime CreatedAt, DateTime UpdatedAt,long ParkingSpaceCode, string ParkingSpaceName,long? AssignedUnitCode,string? AssignedUnitNo);

    public class UpdateParkingSlotByIdHandler : ICommandHandler<UpdateParkingSlotByIdCommand, UpdateParkingSlotByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateParkingSlotByIdHandler> _logger;

        public UpdateParkingSlotByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateParkingSlotByIdHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<UpdateParkingSlotByIdResult> Handle(UpdateParkingSlotByIdCommand request, CancellationToken cancellationToken)
        {
            ParkingSlot? parkingSlot = await _areaDbContext.ParkingSlots.FirstOrDefaultAsync(ps => ps.Id == request.Id, cancellationToken);
            if (parkingSlot == null)
            {
                _logger.LogWarning("Parking slot with Id {Id} not found for update.", request.Id);
                return null;
            }

            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParkingSpaceCode == request.ParkingSpaceCode, cancellationToken);

            if (parkingSpace == null)
            {
                _logger.LogWarning("Parking space with code {ParkingSpaceCode} not found for parking slot update.", request.ParkingSpaceCode);
                return null;
            }

            EntityModels.Unit? assignedUnit = null;
            if (request.AssignedUnitCode.HasValue)
            {
                assignedUnit = await _areaDbContext.Units.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Code == request.AssignedUnitCode.Value, cancellationToken);

                if (assignedUnit == null)
                {
                    _logger.LogWarning("Assigned unit with code {UnitCode} not found for parking slot update.", request.AssignedUnitCode.Value);
                    return null;
                }
            }

            parkingSlot.ParkingSpaceId = parkingSpace.Id;
            parkingSlot.AssignedUnitId = assignedUnit?.Id;
            parkingSlot.SlotType = System.Enum.Parse<SlotType>(request.SlotType, true);
            parkingSlot.Status = System.Enum.Parse<Status>(request.Status, true);
            parkingSlot.UpdatedAt = DateTime.UtcNow;

            await _areaDbContext.SaveChangesAsync(cancellationToken);

            return new UpdateParkingSlotByIdResult(
                parkingSlot.Id ?? Guid.Empty,
                parkingSlot.SlotCode,
                parkingSlot.SlotType.ToString(),
                parkingSlot.Status.ToString(),
                parkingSlot.CreatedAt ?? DateTime.MinValue,
                parkingSlot.UpdatedAt ?? DateTime.MinValue,
                parkingSpace.ParkingSpaceCode,
                parkingSpace.Name ?? string.Empty,
                assignedUnit?.Code,
                assignedUnit?.UnitNo);
        }
    }
}
