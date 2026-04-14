namespace ResidentialAreas.API.ResidentiaAreas.Areas.FilterArea
{
    public record FilterAreaQuery(string? Name, string? City, string? State, string? Country, string? PostalCode, string? Address, string? Status):IQuery<FilterAreaResult>;
    public record FilterAreaResult(List<FilterAreaResponseInstance>? Areas);
    public class FilterAreaHandler : IQueryHandler<FilterAreaQuery, FilterAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<FilterAreaHandler> _logger;
        public FilterAreaHandler(AreaDbContext areaDbContext, ILogger<FilterAreaHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }
        public async Task<FilterAreaResult> Handle(FilterAreaQuery request, CancellationToken cancellationToken)
        {
            Status? statusValue = null;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                statusValue = System.Enum.Parse<Status>(request.Status, true);
            }

            IQueryable<Area> query = _areaDbContext.Areas
                .Where(a => (string.IsNullOrEmpty(request.Name) || a.Name.Contains(request.Name)) &&
                            (string.IsNullOrEmpty(request.City) || a.City.Contains(request.City)) &&
                            (string.IsNullOrEmpty(request.State) || a.State.Contains(request.State)) &&
                            (string.IsNullOrEmpty(request.Country) || a.Country.Contains(request.Country)) &&
                            (string.IsNullOrEmpty(request.PostalCode) || a.PostalCode.Contains(request.PostalCode)) &&
                            (string.IsNullOrEmpty(request.Address) || a.Address.Contains(request.Address)) &&
                            (!statusValue.HasValue || a.Status == statusValue.Value));

            var areas = await query.ToListAsync(cancellationToken);

            return new FilterAreaResult(areas.Select(area => area.Adapt<FilterAreaResponseInstance>()).ToList());
        }
    }
}
