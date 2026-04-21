using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.UpdateFacilityById
{
    public record UpdateFacilityByIdCommand(Guid Id, long? AreaCode, long? BuildingCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateFacilityByIdResult>;
    public record UpdateFacilityByIdResult(Guid Id, long FacilityCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, DateTime CreatedAt, DateTime UpdatedAt, long? AreaCode, string? AreaName, long? BuildingCode, string? BuildingName, List<string?>? ImageUrls);

    public class UpdateFacilityByIdHandler : ICommandHandler<UpdateFacilityByIdCommand, UpdateFacilityByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateFacilityByIdHandler> _logger;
        private readonly IImageSaver _imageSaver;

        public UpdateFacilityByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateFacilityByIdHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<UpdateFacilityByIdResult> Handle(UpdateFacilityByIdCommand request, CancellationToken cancellationToken)
        {
            Facility? facility = await _areaDbContext.Facilities.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
            if (facility == null)
            {
                _logger.LogWarning("Facility with Id {Id} not found for update.", request.Id);
                return null;
            }

            if (request.AreaCode.HasValue && request.BuildingCode.HasValue)
            {
                _logger.LogWarning("Update facility failed. Both AreaCode and BuildingCode were provided for facility {Id}.", request.Id);
                return null;
            }

            Area? area = null;
            Building? building = null;

            if (request.AreaCode.HasValue)
            {
                area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode.Value, cancellationToken);
                if (area == null)
                {
                    _logger.LogWarning("Area with code {AreaCode} not found for facility update.", request.AreaCode.Value);
                    return null;
                }

                facility.AreaId = area.Id;
                facility.BuildingId = null;
            }
            else if (request.BuildingCode.HasValue)
            {
                building = await _areaDbContext.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Code == request.BuildingCode.Value, cancellationToken);
                if (building == null)
                {
                    _logger.LogWarning("Building with code {BuildingCode} not found for facility update.", request.BuildingCode.Value);
                    return null;
                }

                facility.BuildingId = building.Id;
                facility.AreaId = null;
            }

            facility.Name = request.Name;
            facility.FacilityType = request.FacilityType;
            facility.Capacity = request.Capacity;
            facility.BookingRequired = request.BookingRequired;
            facility.HourlyRate = request.HourlyRate;
            facility.Rules = request.Rules;
            facility.Status = System.Enum.Parse<Status>(request.Status, true);
            facility.UpdatedAt = DateTime.UtcNow;

            long facilityCode = facility.FacilityCode ?? 0;

            List<string?>? existingImageUrls = await _areaDbContext.Images
                .Where(i => i.FacilityCode == facilityCode && i.ImageType == ImageType.Facility)
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
                .Where(i => i.FacilityCode == facilityCode && i.ImageType == ImageType.Facility && imagesToRemove.Contains(i.Url))
                .ExecuteDeleteAsync(cancellationToken);

            string imagePath = string.Empty;
            foreach (string? base64Image in request.AddedBase64StringImages ?? [])
            {
                if (!string.IsNullOrEmpty(base64Image))
                {
                    try
                    {
                        imagePath = await _imageSaver.SaveImageAsync(base64Image, "wwwroot/images/Facilities");
                    }
                    catch
                    {
                        _logger.LogError("Failed to save image for facility with code {FacilityCode}", facilityCode);
                        imagePath = "images/default.jpg";
                    }

                    _areaDbContext.Images.Add(new Image
                    {
                        Id = Guid.NewGuid(),
                        FacilityCode = facilityCode,
                        ImageType = ImageType.Facility,
                        Url = imagePath
                    });
                }
            }

            await _areaDbContext.SaveChangesAsync(cancellationToken);

            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.FacilityCode == facilityCode && i.ImageType == ImageType.Facility)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);

            area ??= await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == facility.AreaId, cancellationToken);
            building ??= await _areaDbContext.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == facility.BuildingId, cancellationToken);

            return new UpdateFacilityByIdResult(
                facility.Id ?? Guid.Empty,
                facilityCode,
                facility.Name ?? string.Empty,
                facility.FacilityType ?? string.Empty,
                facility.Capacity ?? 0,
                facility.BookingRequired ?? false,
                facility.HourlyRate,
                facility.Rules,
                facility.Status.ToString(),
                facility.CreatedAt ?? DateTime.MinValue,
                facility.UpdatedAt ?? DateTime.MinValue,
                area?.Code,
                area?.Name,
                building?.Code,
                building?.Name,
                allImageUrls);
        }
    }
}
