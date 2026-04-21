namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.FilterParkingSpace
{
    public record FilterParkingSpaceQuery(long? AreaCode, string? Name, string? BlockNo, string? Status) : IQuery<FilterParkingSpaceResult>;
    public record FilterParkingSpaceResult(List<FilterParkingSpaceResponseInstance>? ParkingSpaces);

    public class FilterParkingSpaceHandler : IQueryHandler<FilterParkingSpaceQuery, FilterParkingSpaceResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<FilterParkingSpaceHandler> _logger;

        public FilterParkingSpaceHandler(AreaDbContext areaDbContext, ILogger<FilterParkingSpaceHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<FilterParkingSpaceResult> Handle(FilterParkingSpaceQuery request, CancellationToken cancellationToken)
        {
            Status? statusValue = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                statusValue = System.Enum.Parse<Status>(request.Status, true);
            }

            var parkingSpacesQuery = _areaDbContext.ParkingSpaces
                .AsNoTracking()
                .AsQueryable();

            if (request.AreaCode.HasValue)
            {
                parkingSpacesQuery = parkingSpacesQuery.Where(p => p.Area != null && p.Area.Code == request.AreaCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                parkingSpacesQuery = parkingSpacesQuery.Where(p => p.Name != null && p.Name.Contains(request.Name));
            }

            if (!string.IsNullOrWhiteSpace(request.BlockNo))
            {
                parkingSpacesQuery = parkingSpacesQuery.Where(p => p.BlockNo != null && p.BlockNo.Contains(request.BlockNo));
            }

            if (statusValue.HasValue)
            {
                parkingSpacesQuery = parkingSpacesQuery.Where(p => p.Status == statusValue.Value);
            }

            var parkingSpaces = await parkingSpacesQuery
                .Select(p => new FilterParkingSpaceResponseInstance(
                    p.Id,
                    p.ParkingSpaceCode,
                    p.Name ?? string.Empty,
                    p.Description,
                    p.BlockNo ?? string.Empty,
                    p.Status.ToString(),
                    p.Area != null ? p.Area.Code : 0,
                    p.Area != null ? p.Area.Name ?? string.Empty : string.Empty,
                    p.Images != null ? p.Images.Select(i => i.Url).ToList() : new List<string?>()))
                .ToListAsync(cancellationToken);

            return new FilterParkingSpaceResult(parkingSpaces);
        }
    }
}
