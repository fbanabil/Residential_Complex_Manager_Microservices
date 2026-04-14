using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.UpdateAreaByCode
{
    public record UpdateAreaByCodeCommand(long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status) : ICommand<UpdateAreaByCodeResult>;

    public record UpdateAreaByCodeResult(Guid Id,long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status);


    public class UpdateAreaByCodeHandler : ICommandHandler<UpdateAreaByCodeCommand, UpdateAreaByCodeResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateAreaByCodeHandler> _logger;
        public UpdateAreaByCodeHandler(AreaDbContext areaDbContext, ILogger<UpdateAreaByCodeHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }
        public async Task<UpdateAreaByCodeResult> Handle(UpdateAreaByCodeCommand request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.FirstOrDefaultAsync(a => a.Code == request.Code, cancellationToken);

            if (area == null)
            {
                return null;
            }

            area.Name = request.Name;
            area.City = request.City;
            area.State = request.State;
            area.Country = request.Country;
            area.PostalCode = request.PostalCode;
            area.Address = request.Address;
            area.GeoBoundary = request.GeoBoundary;
            area.Status = (Status)System.Enum.Parse(typeof(Status), request.Status, true);
            area.UpdatedAt = DateTime.UtcNow;
            await _areaDbContext.SaveChangesAsync(cancellationToken);

            return new UpdateAreaByCodeResult(area.Id, area.Code, area.Name, area.City, area.State, area.Country, area.PostalCode, area.Address, area.GeoBoundary, area.Status.ToString());
        }
    }
}
