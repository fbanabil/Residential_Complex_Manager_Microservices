namespace ResidentialAreas.API.ResidentiaAreas.Buildings.FilterBuilding
{
    public record FilterBuildingQuery(long? AreaCode, string? Name, string? BlockNo, int? TotalFloors, string? Status) : IQuery<FilterBuildingResult>;
    public record FilterBuildingResult(List<FilterBuildingResponseInstance>? Buildings);

    public class FilterBuildingHandler : IQueryHandler<FilterBuildingQuery, FilterBuildingResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<FilterBuildingHandler> _logger;

        public FilterBuildingHandler(AreaDbContext areaDbContext, ILogger<FilterBuildingHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<FilterBuildingResult> Handle(FilterBuildingQuery request, CancellationToken cancellationToken)
        {
            Status? statusValue = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                statusValue = System.Enum.Parse<Status>(request.Status, true);
            }

            var query = await _areaDbContext.Buildings.Include(b => b.Images).Include(b => b.Area).AsNoTracking()
                .Select(a=>new
                {
                    a.Id,
                    a.Name,
                    a.Code,
                    a.BlockNo,
                    a.TotalFloors,
                    a.Status,
                    a.Address,
                    Area = new
                    {
                        a.Area!.Code,
                        a.Area.Name
                    },
                    Images = a.Images.Select(i => new { i.Url }).ToList()
                })
                .Where(b => (string.IsNullOrEmpty(request.Name) || b.Name!.Contains(request.Name)) &&
                            (string.IsNullOrEmpty(request.BlockNo) || b.BlockNo!.Contains(request.BlockNo)) &&
                            (!request.TotalFloors.HasValue || b.TotalFloors == request.TotalFloors.Value) &&
                            (!statusValue.HasValue || b.Status == statusValue.Value) &&
                            (!request.AreaCode.HasValue || b.Area!.Code == request.AreaCode.Value)).ToListAsync(cancellationToken);


            return new FilterBuildingResult(query.Select(b=> b.Adapt<FilterBuildingResponseInstance>() with {ImageUrls = b.Images?.Select(i => i.Url).ToList(), AreaName = b.Area?.Name ?? string.Empty, AreaCode = b.Area?.Code ?? 0 }).ToList());
        }
    }
}