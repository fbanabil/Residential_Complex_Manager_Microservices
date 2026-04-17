using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.UpdateBuildingByCode
{
    public record UpdateBuildingByCodeCommand(long Code, string Name, string BlockNo, int TotalFloors, string Address, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateBuildingByCodeResult>;
    public record UpdateBuildingByCodeResult(Guid Id, long Code, string Name, string BlockNo, int? TotalFloors, string Address, string Status, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class UpdateBuildingByCodeHandler : ICommandHandler<UpdateBuildingByCodeCommand, UpdateBuildingByCodeResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateBuildingByCodeHandler> _logger;
        private readonly IImageSaver _imageSaver;

        public UpdateBuildingByCodeHandler(AreaDbContext areaDbContext, ILogger<UpdateBuildingByCodeHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<UpdateBuildingByCodeResult> Handle(UpdateBuildingByCodeCommand request, CancellationToken cancellationToken)
        {
            Building? building = await _areaDbContext.Buildings.FirstOrDefaultAsync(b => b.Code == request.Code, cancellationToken);
            if (building == null)
            {
                _logger.LogWarning("Building with Code {Code} not found for update.", request.Code);
                return null;
            }

            building.Name = request.Name;
            building.BlockNo = request.BlockNo;
            building.TotalFloors = request.TotalFloors;
            building.Address = request.Address;
            building.Status = (Status)System.Enum.Parse(typeof(Status), request.Status, true);
            building.UpdatedAt = DateTime.UtcNow;

            List<string?>? existingImageUrls = await _areaDbContext.Images
                .Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);

            List<string?>? removedImagePaths = request.RemovedImagesUrls?.Select(url => url != null && url.Contains("images/") ? "images/" + url.Split("images/").LastOrDefault() : url).ToList();
            List<string?>? imagesToRemove = existingImageUrls.Where(url => removedImagePaths != null && removedImagePaths.Contains(url)).ToList();

            await _imageSaver.DeleteImages(imagesToRemove);
            await _areaDbContext.Images.Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building && imagesToRemove.Contains(i.Url)).ExecuteDeleteAsync(cancellationToken);

            string imagePath = string.Empty;
            foreach (string? base64Image in request.AddedBase64StringImages ?? [])
            {
                if (!string.IsNullOrEmpty(base64Image))
                {
                    try
                    {
                        imagePath = await _imageSaver.SaveImageAsync(base64Image, "wwwroot/images/Buildings");
                    }
                    catch
                    {
                        _logger.LogError("Failed to save image for building with code {BuildingCode}", building.Code);
                        imagePath = "images/default.jpg";
                    }

                    _areaDbContext.Images.Add(new Image { BuildingCode = building.Code, ImageType = ImageType.Building, Url = imagePath });
                }
            }

            await _areaDbContext.SaveChangesAsync(cancellationToken);

            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);

            var areaInfo = await _areaDbContext.Areas.AsNoTracking()
                .Where(a => a.Id == building.AreaId)
                .Select(a => new { a.Code, a.Name })
                .FirstOrDefaultAsync(cancellationToken);

            return new UpdateBuildingByCodeResult(building.Id!.Value, building.Code, building.Name ?? string.Empty, building.BlockNo ?? string.Empty, building.TotalFloors, building.Address ?? string.Empty, building.Status.ToString(), areaInfo?.Code ?? 0, areaInfo?.Name ?? string.Empty, allImageUrls);
        }
    }
}