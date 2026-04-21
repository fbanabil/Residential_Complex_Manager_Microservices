namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.GetParkingSpaceById
{
    public record GetParkingSpaceByIdQuery(Guid Id) : IQuery<GetParkingSpaceByIdResult>;
    public record GetParkingSpaceByIdResult(Guid Id, long ParkingSpaceCode, string Name, string? Description, string BlockNo, string Status, DateTime CreatedAt, DateTime UpdatedAt, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class GetParkingSpaceByIdHandler : IQueryHandler<GetParkingSpaceByIdQuery, GetParkingSpaceByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetParkingSpaceByIdHandler> _logger;

        public GetParkingSpaceByIdHandler(AreaDbContext areaDbContext, ILogger<GetParkingSpaceByIdHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<GetParkingSpaceByIdResult> Handle(GetParkingSpaceByIdQuery request, CancellationToken cancellationToken)
        {
            var parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking()
                .Where(p => p.Id == request.Id)
                .Select(p => new
                {
                    p.Id,
                    p.ParkingSpaceCode,
                    p.Name,
                    p.Description,
                    p.BlockNo,
                    Status = p.Status.ToString(),
                    p.CreatedAt,
                    p.UpdatedAt,
                    AreaCode = p.Area!.Code,
                    AreaName = p.Area!.Name,
                    ImageUrls = p.Images.Select(i => i.Url).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (parkingSpace == null)
            {
                _logger.LogWarning("Parking space with ID {Id} not found.", request.Id);
                return null;
            }

            return new GetParkingSpaceByIdResult(
                parkingSpace.Id,
                parkingSpace.ParkingSpaceCode,
                parkingSpace.Name ?? string.Empty,
                parkingSpace.Description,
                parkingSpace.BlockNo ?? string.Empty,
                parkingSpace.Status,
                parkingSpace.CreatedAt ?? DateTime.MinValue,
                parkingSpace.UpdatedAt ?? DateTime.MinValue,
                parkingSpace.AreaCode,
                parkingSpace.AreaName ?? string.Empty,
                parkingSpace.ImageUrls);
        }
    }
}
