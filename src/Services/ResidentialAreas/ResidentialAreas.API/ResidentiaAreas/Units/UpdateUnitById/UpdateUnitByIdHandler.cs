using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Units.UpdateUnitById
{
    public record UpdateUnitByIdCommand(Guid Id, long BuildingCode, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateUnitByIdResult>;

    public record UpdateUnitByIdResult(Guid Id, long Code, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, DateTime CreatedAt, DateTime UpdatedAt, long BuildingCode, string BuildingName, List<string?>? ImageUrls);

    public class UpdateUnitByIdHandler : ICommandHandler<UpdateUnitByIdCommand, UpdateUnitByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateUnitByIdHandler> _logger;
        private readonly IImageSaver _imageSaver;

        public UpdateUnitByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateUnitByIdHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<UpdateUnitByIdResult> Handle(UpdateUnitByIdCommand request, CancellationToken cancellationToken)
        {
            EntityModels.Unit? unit = await _areaDbContext.Units.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
            if (unit == null)
            {
                _logger.LogWarning("Unit with Id {Id} not found for update.", request.Id);
                return null;
            }

            Building? building = await _areaDbContext.Buildings.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Code == request.BuildingCode, cancellationToken);

            if (building == null)
            {
                _logger.LogWarning("Building with code {BuildingCode} not found for unit update.", request.BuildingCode);
                return null;
            }

            bool duplicateUnitNo = await _areaDbContext.Units.AsNoTracking()
                .AnyAsync(u => u.Id != request.Id && u.BuildingId == building.Id && u.UnitNo == request.UnitNo, cancellationToken);

            if (duplicateUnitNo)
            {
                _logger.LogWarning("Unit number {UnitNo} already exists in building {BuildingCode}.", request.UnitNo, request.BuildingCode);
                return null;
            }

            unit.BuildingId = building.Id!.Value;
            unit.UnitNo = request.UnitNo;
            unit.FloorNo = request.FloorNo;
            unit.UnitType = System.Enum.Parse<UnitType>(request.UnitType, true);
            unit.Bedrooms = request.Bedrooms;
            unit.Bathrooms = request.Bathrooms;
            unit.AreaSqft = request.AreaSqft;
            unit.OccupancyStatus = System.Enum.Parse<OccupancyStatus>(request.OccupancyStatus, true);
            unit.OwnershipType = System.Enum.Parse<OwnershipType>(request.OwnershipType, true);
            unit.CurrentLeaseId = request.CurrentLeaseId;
            unit.UpdatedAt = DateTime.UtcNow;

            List<string?>? existingImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.UnitCode == unit.Code && i.ImageType == ImageType.Unit)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);

            List<string?>? removedImagePaths = request.RemovedImagesUrls?
                .Select(url => url != null && url.Contains("images/") ? "images/" + url.Split("images/").LastOrDefault() : url)
                .ToList();

            List<string?>? imagesToRemove = existingImageUrls
                .Where(url => removedImagePaths != null && removedImagePaths.Contains(url))
                .ToList();

            await _imageSaver.DeleteImages(imagesToRemove);

            await _areaDbContext.Images
                .Where(i => i.UnitCode == unit.Code && i.ImageType == ImageType.Unit && imagesToRemove.Contains(i.Url))
                .ExecuteDeleteAsync(cancellationToken);

            foreach (string? base64Image in request.AddedBase64StringImages ?? [])
            {
                if (string.IsNullOrWhiteSpace(base64Image))
                {
                    continue;
                }

                string imagePath = string.Empty;
                try
                {
                    imagePath = await _imageSaver.SaveImageAsync(base64Image, "wwwroot/images/Units");
                }
                catch
                {
                    _logger.LogError("Failed to save image for unit with code {UnitCode}", unit.Code);
                    imagePath = "images/default.jpg";
                }

                _areaDbContext.Images.Add(new Image
                {
                    Id = Guid.NewGuid(),
                    UnitCode = unit.Code,
                    ImageType = ImageType.Unit,
                    Url = imagePath
                });
            }

            await _areaDbContext.SaveChangesAsync(cancellationToken);

            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.UnitCode == unit.Code && i.ImageType == ImageType.Unit)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);

            return new UpdateUnitByIdResult(
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
                unit.CreatedAt,
                unit.UpdatedAt,
                building.Code,
                building.Name ?? string.Empty,
                allImageUrls);
        }
    }
}
