
using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.GetAreaByCode
{
    public record GetAreaByCodeQuery(long Code) : IQuery<GetAreaByCodeResult>;
    public record GetAreaByCodeResult(Guid Id, long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, DateTime CreatedAt, DateTime UpdatedAt);

    public class GetAreaByCodeHandler : IQueryHandler<GetAreaByCodeQuery, GetAreaByCodeResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<GetAreaByCodeHandler> _logger;

        public GetAreaByCodeHandler(AreaDbContext areaDbContext, ILogger<GetAreaByCodeHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }


        public async Task<GetAreaByCodeResult> Handle(GetAreaByCodeQuery request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.FirstOrDefaultAsync(a => a.Code == request.Code, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Area with code {Code} not found.", request.Code);
                return null;
            }

            GetAreaByCodeResult result = new GetAreaByCodeResult(
                Id: area.Id,
                Code: area.Code,
                Name: area.Name,
                City: area.City,
                State: area.State,
                Country: area.Country,
                PostalCode: area.PostalCode,
                Address: area.Address,
                GeoBoundary: area.GeoBoundary,
                Status: area.Status.ToString(),
                CreatedAt: area.CreatedAt,
                UpdatedAt: area.UpdatedAt
            );

            return result;
        }
    }
}
