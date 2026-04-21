namespace ResidentialAreas.API.ResidentiaAreas.Units.FilterUnit
{
    public record FilterUnitQuery(long? BuildingCode, string? UnitNo, int? FloorNo, string? UnitType, string? OccupancyStatus, string? OwnershipType) : IQuery<FilterUnitResult>;

    public record FilterUnitResult(List<FilterUnitResponseInstance>? Units);

    public class FilterUnitHandler : IQueryHandler<FilterUnitQuery, FilterUnitResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<FilterUnitHandler> _logger;

        public FilterUnitHandler(AreaDbContext areaDbContext, ILogger<FilterUnitHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<FilterUnitResult> Handle(FilterUnitQuery request, CancellationToken cancellationToken)
        {
            UnitType? unitType = null;
            OccupancyStatus? occupancyStatus = null;
            OwnershipType? ownershipType = null;

            if (!string.IsNullOrWhiteSpace(request.UnitType))
            {
                unitType = System.Enum.Parse<UnitType>(request.UnitType, true);
            }

            if (!string.IsNullOrWhiteSpace(request.OccupancyStatus))
            {
                occupancyStatus = System.Enum.Parse<OccupancyStatus>(request.OccupancyStatus, true);
            }

            if (!string.IsNullOrWhiteSpace(request.OwnershipType))
            {
                ownershipType = System.Enum.Parse<OwnershipType>(request.OwnershipType, true);
            }

            var unitsQuery = _areaDbContext.Units.AsNoTracking().AsQueryable();

            if (request.BuildingCode.HasValue)
            {
                unitsQuery = unitsQuery.Where(u => u.Building != null && u.Building.Code == request.BuildingCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.UnitNo))
            {
                unitsQuery = unitsQuery.Where(u => u.UnitNo.Contains(request.UnitNo));
            }

            if (request.FloorNo.HasValue)
            {
                unitsQuery = unitsQuery.Where(u => u.FloorNo == request.FloorNo.Value);
            }

            if (unitType.HasValue)
            {
                unitsQuery = unitsQuery.Where(u => u.UnitType == unitType.Value);
            }

            if (occupancyStatus.HasValue)
            {
                unitsQuery = unitsQuery.Where(u => u.OccupancyStatus == occupancyStatus.Value);
            }

            if (ownershipType.HasValue)
            {
                unitsQuery = unitsQuery.Where(u => u.OwnershipType == ownershipType.Value);
            }

            var units = await unitsQuery
                .Select(u => new FilterUnitResponseInstance(
                    u.Id,
                    u.Code,
                    u.UnitNo,
                    u.FloorNo,
                    u.UnitType.ToString(),
                    u.Bedrooms,
                    u.Bathrooms,
                    u.AreaSqft,
                    u.OccupancyStatus.ToString(),
                    u.OwnershipType.ToString(),
                    u.Building != null ? u.Building.Code : 0,
                    u.Building != null ? u.Building.Name ?? string.Empty : string.Empty,
                    u.Images != null ? u.Images.Select(i => i.Url).ToList() : new List<string?>()))
                .ToListAsync(cancellationToken);

            return new FilterUnitResult(units);
        }
    }
}
