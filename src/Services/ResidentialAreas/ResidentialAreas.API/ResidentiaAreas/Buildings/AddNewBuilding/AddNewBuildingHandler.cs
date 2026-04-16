
using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AddNewBuilding
{

    //public record AddNewBuildingCommand(long AreaCode, string Name, string BlockNo, int TotalFloors, string Address, string Status, List<string?>? ImageBase64) : ICommand<AddNewBuildingResult>;

    //public record AddNewBuildingResult(Guid Id, long Code, string Name, string AreaName, long AreaCode);

    public class AddNewBuildingHandler : ICommandHandler<AddNewBuildingCommand, AddNewBuildingResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewBuildingHandler> _logger;
        private readonly IImageSaver _imageSaver;

        public AddNewBuildingHandler(AreaDbContext areaDbContext, ILogger<AddNewBuildingHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<AddNewBuildingResult> Handle(AddNewBuildingCommand request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);

            if(area == null)
            {
                _logger.LogWarning("Area with code {AreaCode} not found.", request.AreaCode);
                return null;            }

            List<string?>? imagePaths = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/Buildings");


            Building building = new Building
            {
                Id = Guid.NewGuid(),
                AreaId= area!.Id,
                Name = request.Name,
                BlockNo = request.BlockNo,
                TotalFloors = request.TotalFloors,
                Address = request.Address,
                Status = System.Enum.Parse<Status>(request.Status),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _areaDbContext.Buildings.AddAsync(building, cancellationToken);

            await _areaDbContext.SaveChangesAsync(cancellationToken);

            if (imagePaths != null && imagePaths.Count > 0)
            {
                List<Image> imgs = imagePaths.Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    Url = path,
                    ImageType = ImageType.Building,
                    BuildingCode = building.Code,
                }).ToList();
                await _areaDbContext.Images.AddRangeAsync(imgs);
                await _areaDbContext.SaveChangesAsync(cancellationToken);

            }
            return new AddNewBuildingResult(building.Id.Value, building.Code, building.Name!, area.Name, area.Code);

        }
    }
}
