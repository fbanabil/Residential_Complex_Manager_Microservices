namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.FilterParkingSlot
{
    public record FilterParkingSlotQuery( long? ParkingSpaceCode,long? AssignedUnitCode,long? SlotCode,string? SlotType, string? Status) : IQuery<FilterParkingSlotResult>;

    public record FilterParkingSlotResult(List<FilterParkingSlotResponseInstance>? ParkingSlots);

    public class FilterParkingSlotHandler : IQueryHandler<FilterParkingSlotQuery, FilterParkingSlotResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<FilterParkingSlotHandler> _logger;

        public FilterParkingSlotHandler(AreaDbContext areaDbContext, ILogger<FilterParkingSlotHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<FilterParkingSlotResult> Handle(FilterParkingSlotQuery request, CancellationToken cancellationToken)
        {
            SlotType? slotType = null;
            Status? status = null;

            if (!string.IsNullOrWhiteSpace(request.SlotType))
            {
                slotType = System.Enum.Parse<SlotType>(request.SlotType, true);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                status = System.Enum.Parse<Status>(request.Status, true);
            }

            var parkingSlotsQuery = _areaDbContext.ParkingSlots.AsNoTracking().AsQueryable();

            if (request.ParkingSpaceCode.HasValue)
            {
                parkingSlotsQuery = parkingSlotsQuery.Where(ps => ps.ParkingSpace != null && ps.ParkingSpace.ParkingSpaceCode == request.ParkingSpaceCode.Value);
            }

            if (request.AssignedUnitCode.HasValue)
            {
                parkingSlotsQuery = parkingSlotsQuery.Where(ps => ps.AssignedUnit != null && ps.AssignedUnit.Code == request.AssignedUnitCode.Value);
            }

            if (request.SlotCode.HasValue)
            {
                parkingSlotsQuery = parkingSlotsQuery.Where(ps => ps.SlotCode == request.SlotCode.Value);
            }

            if (slotType.HasValue)
            {
                parkingSlotsQuery = parkingSlotsQuery.Where(ps => ps.SlotType == slotType.Value);
            }

            if (status.HasValue)
            {
                parkingSlotsQuery = parkingSlotsQuery.Where(ps => ps.Status == status.Value);
            }

            var parkingSlots = await parkingSlotsQuery
                .Select(ps => new FilterParkingSlotResponseInstance(
                    ps.Id ?? Guid.Empty,
                    ps.SlotCode,
                    ps.SlotType.ToString(),
                    ps.Status.ToString(),
                    ps.ParkingSpace != null ? ps.ParkingSpace.ParkingSpaceCode : 0,
                    ps.ParkingSpace != null ? ps.ParkingSpace.Name ?? string.Empty : string.Empty,
                    ps.AssignedUnit != null ? (long?)ps.AssignedUnit.Code : null,
                    ps.AssignedUnit != null ? ps.AssignedUnit.UnitNo : null))
                .ToListAsync(cancellationToken);

            return new FilterParkingSlotResult(parkingSlots);
        }
    }
}
