namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.GetParkingSlotById
{
    public record GetParkingSlotByIdQuery(Guid Id) : IQuery<GetParkingSlotByIdResult>;


    public record GetParkingSlotByIdResult(Guid Id, long SlotCode, string SlotType, string Status, DateTime CreatedAt,DateTime UpdatedAt, long ParkingSpaceCode,string ParkingSpaceName,long? AssignedUnitCode,string? AssignedUnitNo);

    public class GetParkingSlotByIdHandler : IQueryHandler<GetParkingSlotByIdQuery, GetParkingSlotByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetParkingSlotByIdHandler> _logger;

        public GetParkingSlotByIdHandler(AreaDbContext areaDbContext, ILogger<GetParkingSlotByIdHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<GetParkingSlotByIdResult> Handle(GetParkingSlotByIdQuery request, CancellationToken cancellationToken)
        {
            var parkingSlot = await _areaDbContext.ParkingSlots.AsNoTracking()
                .Where(ps => ps.Id == request.Id)
                .Select(ps => new
                {
                    ps.Id,
                    ps.SlotCode,
                    SlotType = ps.SlotType.ToString(),
                    Status = ps.Status.ToString(),
                    ps.CreatedAt,
                    ps.UpdatedAt,
                    ParkingSpaceCode = ps.ParkingSpace != null ? ps.ParkingSpace.ParkingSpaceCode : 0,
                    ParkingSpaceName = ps.ParkingSpace != null ? ps.ParkingSpace.Name : string.Empty,
                    AssignedUnitCode = ps.AssignedUnit != null ? (long?)ps.AssignedUnit.Code : null,
                    AssignedUnitNo = ps.AssignedUnit != null ? ps.AssignedUnit.UnitNo : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (parkingSlot == null)
            {
                _logger.LogWarning("Parking slot with ID {Id} not found.", request.Id);
                return null;
            }

            return new GetParkingSlotByIdResult(
                parkingSlot.Id ?? Guid.Empty,
                parkingSlot.SlotCode,
                parkingSlot.SlotType,
                parkingSlot.Status,
                parkingSlot.CreatedAt ?? DateTime.MinValue,
                parkingSlot.UpdatedAt ?? DateTime.MinValue,
                parkingSlot.ParkingSpaceCode,
                parkingSlot.ParkingSpaceName ?? string.Empty,
                parkingSlot.AssignedUnitCode,
                parkingSlot.AssignedUnitNo);
        }
    }
}
