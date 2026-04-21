using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AddNewFacility
{
    public record AddNewFacilityCommand(long? AreaCode, long? BuildingCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, List<string?>? ImageBase64) : ICommand<AddNewFacilityResult>;
    public record AddNewFacilityResult(Guid Id, long FacilityCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, long? AreaCode, string? AreaName, long? BuildingCode, string? BuildingName, List<string?>? ImageUrls);

    public class AddNewFacilityHandler : ICommandHandler<AddNewFacilityCommand, AddNewFacilityResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewFacilityHandler> _logger;
        private readonly IImageSaver _imageSaver;

        public AddNewFacilityHandler(AreaDbContext areaDbContext, ILogger<AddNewFacilityHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<AddNewFacilityResult> Handle(AddNewFacilityCommand request, CancellationToken cancellationToken)
        {
            if (!request.AreaCode.HasValue && !request.BuildingCode.HasValue)
            {
                _logger.LogWarning("Create facility failed. Neither AreaCode nor BuildingCode was provided.");
                return null;
            }

            if (request.AreaCode.HasValue && request.BuildingCode.HasValue)
            {
                _logger.LogWarning("Create facility failed. Both AreaCode and BuildingCode were provided.");
                return null;
            }

            Area? area = null;
            Building? building = null;

            if (request.AreaCode.HasValue)
            {
                area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode.Value, cancellationToken);
                if (area == null)
                {
                    _logger.LogWarning("Area with code {AreaCode} not found while creating facility.", request.AreaCode.Value);
                    return null;
                }
            }

            if (request.BuildingCode.HasValue)
            {
                building = await _areaDbContext.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Code == request.BuildingCode.Value, cancellationToken);
                if (building == null)
                {
                    _logger.LogWarning("Building with code {BuildingCode} not found while creating facility.", request.BuildingCode.Value);
                    return null;
                }
            }

            List<string?>? imagePaths = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/Facilities");

            Facility facility = new Facility
            {
                Id = Guid.NewGuid(),
                AreaId = area?.Id,
                BuildingId = building?.Id,
                Name = request.Name,
                FacilityType = request.FacilityType,
                Capacity = request.Capacity,
                BookingRequired = request.BookingRequired,
                HourlyRate = request.HourlyRate,
                Rules = request.Rules,
                Status = System.Enum.Parse<Status>(request.Status, true),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _areaDbContext.Facilities.AddAsync(facility, cancellationToken);
            await _areaDbContext.SaveChangesAsync(cancellationToken);

            if (facility.FacilityCode.HasValue && imagePaths != null && imagePaths.Count > 0)
            {
                List<Image> images = imagePaths.Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    Url = path,
                    ImageType = ImageType.Facility,
                    FacilityCode = facility.FacilityCode.Value
                }).ToList();

                await _areaDbContext.Images.AddRangeAsync(images, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }

            return new AddNewFacilityResult(
                facility.Id!.Value,
                facility.FacilityCode ?? 0,
                facility.Name ?? string.Empty,
                facility.FacilityType ?? string.Empty,
                facility.Capacity ?? 0,
                facility.BookingRequired ?? false,
                facility.HourlyRate,
                facility.Rules,
                facility.Status.ToString(),
                area?.Code,
                area?.Name,
                building?.Code,
                building?.Name,
                imagePaths);
        }
    }
}
