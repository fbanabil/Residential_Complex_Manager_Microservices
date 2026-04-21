namespace ResidentialAreas.API.ResidentiaAreas.Facilities.FilterFacility
{
    public record FilterFacilityQuery(long? AreaCode, long? BuildingCode, string? Name, string? FacilityType, int? Capacity, bool? BookingRequired, string? Status) : IQuery<FilterFacilityResult>;
    public record FilterFacilityResult(List<FilterFacilityResponseInstance>? Facilities);

    public class FilterFacilityHandler : IQueryHandler<FilterFacilityQuery, FilterFacilityResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<FilterFacilityHandler> _logger;

        public FilterFacilityHandler(AreaDbContext areaDbContext, ILogger<FilterFacilityHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<FilterFacilityResult> Handle(FilterFacilityQuery request, CancellationToken cancellationToken)
        {
            Status? statusValue = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                statusValue = System.Enum.Parse<Status>(request.Status, true);
            }

            var facilitiesQuery = _areaDbContext.Facilities
                .AsNoTracking()
                .AsQueryable();

            if (request.AreaCode.HasValue)
            {
                facilitiesQuery = facilitiesQuery.Where(f => f.Area != null && f.Area.Code == request.AreaCode.Value);
            }

            if (request.BuildingCode.HasValue)
            {
                facilitiesQuery = facilitiesQuery.Where(f => f.Building != null && f.Building.Code == request.BuildingCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                facilitiesQuery = facilitiesQuery.Where(f => f.Name != null && f.Name.Contains(request.Name));
            }

            if (!string.IsNullOrWhiteSpace(request.FacilityType))
            {
                facilitiesQuery = facilitiesQuery.Where(f => f.FacilityType != null && f.FacilityType.Contains(request.FacilityType));
            }

            if (request.Capacity.HasValue)
            {
                facilitiesQuery = facilitiesQuery.Where(f => f.Capacity == request.Capacity.Value);
            }

            if (request.BookingRequired.HasValue)
            {
                facilitiesQuery = facilitiesQuery.Where(f => f.BookingRequired == request.BookingRequired.Value);
            }

            if (statusValue.HasValue)
            {
                facilitiesQuery = facilitiesQuery.Where(f => f.Status == statusValue.Value);
            }

            var facilities = await facilitiesQuery
                .Select(f => new FilterFacilityResponseInstance(
                    f.Id ?? Guid.Empty,
                    f.FacilityCode ?? 0,
                    f.Name ?? string.Empty,
                    f.FacilityType ?? string.Empty,
                    f.Capacity ?? 0,
                    f.BookingRequired ?? false,
                    f.HourlyRate,
                    f.Rules,
                    f.Status.ToString(),
                    f.Area != null ? f.Area.Code : null,
                    f.Area != null ? f.Area.Name : null,
                    f.Building != null ? f.Building.Code : null,
                    f.Building != null ? f.Building.Name : null,
                    f.Images != null ? f.Images.Select(i => i.Url).ToList() : new List<string?>()))
                .ToListAsync(cancellationToken);


            return new FilterFacilityResult(facilities);
        }
    }
}