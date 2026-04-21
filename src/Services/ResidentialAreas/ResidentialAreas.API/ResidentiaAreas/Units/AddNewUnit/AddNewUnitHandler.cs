using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Units.AddNewUnit
{
    public record AddNewUnitCommand(long BuildingCode, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, List<string?>? ImageBase64) : ICommand<AddNewUnitResult>;

    public record AddNewUnitResult(Guid Id, long Code, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, long BuildingCode, string BuildingName, List<string?>? ImageUrls);

    public class AddNewUnitHandler : ICommandHandler<AddNewUnitCommand, AddNewUnitResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewUnitHandler> _logger;
        private readonly IImageSaver _imageSaver;

        public AddNewUnitHandler(AreaDbContext areaDbContext, ILogger<AddNewUnitHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<AddNewUnitResult> Handle(AddNewUnitCommand request, CancellationToken cancellationToken)
        {
            Building? building = await _areaDbContext.Buildings.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Code == request.BuildingCode, cancellationToken);

            if (building == null)
            {
                _logger.LogWarning("Building with code {BuildingCode} not found while creating unit.", request.BuildingCode);
                return null;
            }

            bool unitExists = await _areaDbContext.Units.AsNoTracking()
                .AnyAsync(u => u.BuildingId == building.Id && u.UnitNo == request.UnitNo, cancellationToken);

            if (unitExists)
            {
                _logger.LogWarning("Unit number {UnitNo} already exists in building {BuildingCode}.", request.UnitNo, request.BuildingCode);
                return null;
            }

            List<string?>? imagePaths = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/Units");

            EntityModels.Unit unit = new EntityModels.Unit
            {
                Id = Guid.NewGuid(),
                BuildingId = building.Id!.Value,
                UnitNo = request.UnitNo,
                FloorNo = request.FloorNo,
                UnitType = System.Enum.Parse<UnitType>(request.UnitType, true),
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                AreaSqft = request.AreaSqft,
                OccupancyStatus = System.Enum.Parse<OccupancyStatus>(request.OccupancyStatus, true),
                OwnershipType = System.Enum.Parse<OwnershipType>(request.OwnershipType, true),
                CurrentLeaseId = request.CurrentLeaseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _areaDbContext.Units.AddAsync(unit, cancellationToken);
            await _areaDbContext.SaveChangesAsync(cancellationToken);

            if (imagePaths != null && imagePaths.Count > 0)
            {
                List<Image> images = imagePaths.Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    Url = path,
                    ImageType = ImageType.Unit,
                    UnitCode = unit.Code
                }).ToList();

                await _areaDbContext.Images.AddRangeAsync(images, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }

            return new AddNewUnitResult(
                unit.Id,
                unit.Code,
                unit.UnitNo,
                unit.FloorNo,
                unit.UnitType.ToString(),
                unit.Bedrooms,
                unit.Bathrooms,
                unit.AreaSqft,
                unit.OccupancyStatus.ToString(),
                unit.OwnershipType.ToString(),
                unit.CurrentLeaseId,
                building.Code,
                building.Name ?? string.Empty,
                imagePaths);
        }
    }
}
