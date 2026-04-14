
namespace ResidentialAreas.API.ResidentiaAreas.Areas.UpdateAreaById
{

    public record UpdateAreaByIdCommand(Guid Id, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status) : ICommand<UpdateAreaByIdResult>;

    public record UpdateAreaByIdResult(Guid Id, long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status);


    public class UpdateAreaByIdHandler : ICommandHandler<UpdateAreaByIdCommand, UpdateAreaByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateAreaByIdHandler> _logger;


        public UpdateAreaByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateAreaByIdHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<UpdateAreaByIdResult> Handle(UpdateAreaByIdCommand request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Area with Id {Id} not found for update.", request.Id);
                
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

            _areaDbContext.SaveChanges();

            var result = area.Adapt<UpdateAreaByIdResult>();

            if (result != null) {
                _logger.LogInformation("Area with Id {Id} updated successfully.", request.Id);
            } else {
                _logger.LogError("Failed to map updated Area with Id {Id} to UpdateAreaByIdResult.", request.Id);
            }

            return result;
        }
    }
}
