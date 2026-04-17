namespace ResidentialAreas.API.ResidentiaAreas.Buildings.GetBuildingById
{
    public record GetBuildingByIdQuery(Guid Id) : IQuery<GetBuildingByIdResult>;
    public record GetBuildingByIdResult(Guid Id, long Code, string Name, string BlockNo, int? TotalFloors, string Address, string Status, DateTime CreatedAt, DateTime UpdatedAt, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class GetBuildingByIdHandler : IQueryHandler<GetBuildingByIdQuery, GetBuildingByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetBuildingByIdHandler> _logger;

        public GetBuildingByIdHandler(AreaDbContext areaDbContext, ILogger<GetBuildingByIdHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<GetBuildingByIdResult> Handle(GetBuildingByIdQuery request, CancellationToken cancellationToken)
        {
            var building = await _areaDbContext.Buildings.AsNoTracking()
                .Where(b => b.Id == request.Id)
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
                _logger.LogWarning("Building with ID {Id} not found.", request.Id);
                return null;
            }

            return building.Adapt<GetBuildingByIdResult>();
        }
    }
}
