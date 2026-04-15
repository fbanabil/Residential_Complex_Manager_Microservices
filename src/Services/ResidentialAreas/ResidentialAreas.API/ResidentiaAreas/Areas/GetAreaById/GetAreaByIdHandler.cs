
namespace ResidentialAreas.API.ResidentiaAreas.Areas.GetAreaById
{

    public record GetAreaByIdQuery(Guid Id) : IQuery<GetAreaByIdResult>;
    public record GetAreaByIdResult(Guid Id, long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, DateTime CreatedAt, DateTime UpdatedAt, List<string?>? ImageUrls);

    public class GetAreaByIdHandler : IQueryHandler<GetAreaByIdQuery, GetAreaByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetAreaByIdHandler> _logger;
        public GetAreaByIdHandler(AreaDbContext areaDbContext, ILogger<GetAreaByIdHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<GetAreaByIdResult> Handle(GetAreaByIdQuery request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (area == null)
            {
                _logger.LogWarning("Area with ID {Id} not found.", request.Id);
                return null;
            }


            List<string?>? imageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i=> i.ImageType==ImageType.Area && i.Code == area.Code)
                .Select(ai => ai.Url)
                .ToListAsync(cancellationToken);


            var result = new GetAreaByIdResult(
                area.Id,
                area.Code,
                area.Name,
                area.City,
                area.State,
                area.Country,
                area.PostalCode,
                area.Address,
                area.GeoBoundary,
                area.Status.ToString(),
                area.CreatedAt,
                area.UpdatedAt,
                ImageUrls : imageUrls
            );

            return result;
        }
    }
}
