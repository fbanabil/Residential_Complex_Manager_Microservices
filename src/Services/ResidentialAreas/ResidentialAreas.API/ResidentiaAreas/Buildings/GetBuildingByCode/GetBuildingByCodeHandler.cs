namespace ResidentialAreas.API.ResidentiaAreas.Buildings.GetBuildingByCode
{
    public record GetBuildingByCodeQuery(long Code) : IQuery<GetBuildingByCodeResult>;
    public record GetBuildingByCodeResult(Guid Id, long Code, string Name, string BlockNo, int? TotalFloors, string Address, string Status, DateTime CreatedAt, DateTime UpdatedAt, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class GetBuildingByCodeHandler : IQueryHandler<GetBuildingByCodeQuery, GetBuildingByCodeResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetBuildingByCodeHandler> _logger;

        public GetBuildingByCodeHandler(AreaDbContext areaDbContext, ILogger<GetBuildingByCodeHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<GetBuildingByCodeResult> Handle(GetBuildingByCodeQuery request, CancellationToken cancellationToken)
        {
            var building = await _areaDbContext.Buildings.AsNoTracking()
                .Where(b => b.Code == request.Code)
                .Select(b => new
                {
                    b.Id,
                    b.Code,
                    b.Name,
                    b.BlockNo,
                    b.TotalFloors,
                    b.Address,
                    Status = b.Status.ToString(),
                    b.CreatedAt,
                    b.UpdatedAt,
                    AreaCode = b.Area!.Code,
                    AreaName = b.Area!.Name,
                    ImageUrls = b.Images.Select(i => i.Url).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (building == null)
            {
                _logger.LogWarning("Building with Code {Code} not found.", request.Code);
                return null;
            }

            return building.Adapt<GetBuildingByCodeResult>();
        }
    }
}