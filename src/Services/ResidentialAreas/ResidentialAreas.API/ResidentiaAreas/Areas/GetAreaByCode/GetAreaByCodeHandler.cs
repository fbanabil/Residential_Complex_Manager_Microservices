using ResidentialAreas.API.ResidentiaAreas.Areas.GetAreaById;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.GetAreaByCode
{
    public record GetAreaByCodeQuery(long Code) : IQuery<GetAreaByCodeResult>;
    public record GetAreaByCodeResult(Guid Id, long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, DateTime CreatedAt, DateTime UpdatedAt, List<string?>? ImageUrls);

    public class GetAreaByCodeHandler : IQueryHandler<GetAreaByCodeQuery, GetAreaByCodeResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetAreaByCodeHandler> _logger;

        public GetAreaByCodeHandler(
            AreaDbContext areaDbContext,
            ILogger<GetAreaByCodeHandler> logger ) 
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<GetAreaByCodeResult> Handle(GetAreaByCodeQuery request, CancellationToken cancellationToken)
        {
            var area = await _areaDbContext.Areas.AsNoTracking().Where(a => a.Code == request.Code).Select(a =>
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

            if (area == null)
            {
                _logger.LogWarning("Area with Code {Code} not found.", request.Code);
                return null;
            }

            var result = area.Adapt<GetAreaByCodeResult>();
            return result;
        }
    }
}
