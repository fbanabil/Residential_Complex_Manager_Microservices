namespace ResidentialAreas.API.ResidentiaAreas.Units.GetUnitById
{
    public record GetUnitByIdQuery(Guid Id) : IQuery<GetUnitByIdResult>;

    public record GetUnitByIdResult(Guid Id, long Code, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, DateTime CreatedAt, DateTime UpdatedAt, long BuildingCode, string BuildingName, List<string?>? ImageUrls);
    public class GetUnitByIdHandler : IQueryHandler<GetUnitByIdQuery, GetUnitByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetUnitByIdHandler> _logger;

        public GetUnitByIdHandler(AreaDbContext areaDbContext, ILogger<GetUnitByIdHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<GetUnitByIdResult> Handle(GetUnitByIdQuery request, CancellationToken cancellationToken)
        {
            var unit = await _areaDbContext.Units.AsNoTracking()
                .Where(u => u.Id == request.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Code,
                    u.UnitNo,
                    u.FloorNo,
                    UnitType = u.UnitType.ToString(),
                    u.Bedrooms,
                    u.Bathrooms,
                    u.AreaSqft,
                    OccupancyStatus = u.OccupancyStatus.ToString(),
                    OwnershipType = u.OwnershipType.ToString(),
                    u.CurrentLeaseId,
                    u.CreatedAt,
                    u.UpdatedAt,
                    BuildingCode = u.Building!.Code,
                    BuildingName = u.Building!.Name,
                    ImageUrls = u.Images!.Select(i => i.Url).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (unit == null)
            {
                _logger.LogWarning("Unit with ID {Id} not found.", request.Id);
                return null;
            }

            return new GetUnitByIdResult(
                unit.Id,
                unit.Code,
                unit.UnitNo,
                unit.FloorNo,
                unit.UnitType,
                unit.Bedrooms,
                unit.Bathrooms,
                unit.AreaSqft,
                unit.OccupancyStatus,
                unit.OwnershipType,
                unit.CurrentLeaseId,
                unit.CreatedAt,
                unit.UpdatedAt,
                unit.BuildingCode,
                unit.BuildingName ?? string.Empty,
                unit.ImageUrls);
        }
    }
}
