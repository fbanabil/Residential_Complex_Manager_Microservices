namespace ResidentialAreas.API.ResidentiaAreas.Facilities.GetFacilityById
{
    public record GetFacilityByIdQuery(Guid Id) : IQuery<GetFacilityByIdResult>;
    public record GetFacilityByIdResult(Guid Id, long FacilityCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, DateTime CreatedAt, DateTime UpdatedAt, long? AreaCode, string? AreaName, long? BuildingCode, string? BuildingName, List<string?>? ImageUrls);

    public class GetFacilityByIdHandler : IQueryHandler<GetFacilityByIdQuery, GetFacilityByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetFacilityByIdHandler> _logger;

        public GetFacilityByIdHandler(AreaDbContext areaDbContext, ILogger<GetFacilityByIdHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<GetFacilityByIdResult> Handle(GetFacilityByIdQuery request, CancellationToken cancellationToken)
        {
            var facility = await _areaDbContext.Facilities.AsNoTracking()
                .Where(f => f.Id == request.Id)
                .Select(f => new
                {
                    f.Id,
                    f.FacilityCode,
                    f.Name,
                    f.FacilityType,
                    f.Capacity,
                    f.BookingRequired,
                    f.HourlyRate,
                    f.Rules,
                    Status = f.Status.ToString(),
                    f.CreatedAt,
                    f.UpdatedAt,
                    AreaCode = f.Area != null ? f.Area.Code : (long?)null,
                    AreaName = f.Area != null ? f.Area.Name : null,
                    BuildingCode = f.Building != null ? f.Building.Code : (long?)null,
                    BuildingName = f.Building != null ? f.Building.Name : null,
                    ImageUrls = f.Images.Select(i => i.Url).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (facility == null)
            {
                _logger.LogWarning("Facility with ID {Id} not found.", request.Id);
                return null;
            }

            return new GetFacilityByIdResult(
                facility.Id ?? Guid.Empty,
                facility.FacilityCode ?? 0,
                facility.Name ?? string.Empty,
                facility.FacilityType ?? string.Empty,
                facility.Capacity ?? 0,
                facility.BookingRequired ?? false,
                facility.HourlyRate,
                facility.Rules,
                facility.Status,
                facility.CreatedAt ?? DateTime.MinValue,
                facility.UpdatedAt ?? DateTime.MinValue,
                facility.AreaCode,
                facility.AreaName,
                facility.BuildingCode,
                facility.BuildingName,
                facility.ImageUrls);
        }
    }
}
