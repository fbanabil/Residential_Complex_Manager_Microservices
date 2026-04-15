
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
            var area = await _areaDbContext.Areas.AsNoTracking().Where(a => a.Id == request.Id).Select(a =>
            new {
                a.Id,
                a.Code,
                a.Name,
                a.City,
                a.State,
                a.Country,
                a.PostalCode,
                a.Address,
                a.GeoBoundary,
                Status = a.Status.ToString(),
                a.CreatedAt,
                a.UpdatedAt,
                ImageUrls = a.Images.Select(i => i.Url).ToList()
            }).FirstOrDefaultAsync(cancellationToken);

            if(area == null)
            {
                _logger.LogWarning("Area with ID {Id} not found.", request.Id);
                return null;
            }

            var result = area.Adapt<GetAreaByIdResult>();
            return result;
        }
    }
}
