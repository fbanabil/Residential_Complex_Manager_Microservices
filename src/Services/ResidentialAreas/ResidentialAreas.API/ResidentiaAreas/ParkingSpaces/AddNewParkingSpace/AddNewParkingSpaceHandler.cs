using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.AddNewParkingSpace
{
    public record AddNewParkingSpaceCommand(long AreaCode, string Name, string? Description, string BlockNo, string Status, List<string?>? ImageBase64) : ICommand<AddNewParkingSpaceResult>;
    public record AddNewParkingSpaceResult(AddNewParkingSpaceResponse? Result, ErrorCarrier? ErrorCarrier);

    public class AddNewParkingSpaceHandler : ICommandHandler<AddNewParkingSpaceCommand, AddNewParkingSpaceResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewParkingSpaceHandler> _logger;
        private readonly IImageSaver _imageSaver;

        public AddNewParkingSpaceHandler(AreaDbContext areaDbContext, ILogger<AddNewParkingSpaceHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<AddNewParkingSpaceResult> Handle(AddNewParkingSpaceCommand request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Area with code {AreaCode} not found while creating parking space.", request.AreaCode);
                return new AddNewParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "Area not found",
                    StatusCode = 404,
                    Detail = $"Area with code {request.AreaCode} not found."
                });
            }


            bool existingParkingSpace = await _areaDbContext.ParkingSpaces.AnyAsync(ps => ps.AreaId == area.Id && ps.Name == request.Name, cancellationToken);
            if (existingParkingSpace) {
                _logger.LogWarning("Parking space with name {ParkingSpaceName} already exists in area {AreaCode}.", request.Name, request.AreaCode);
                return new AddNewParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "Duplicate parking space",
                    StatusCode = 400,
                    Detail = $"Parking space with name {request.Name} already exists in area {request.AreaCode}."
                });
            }




            List<string?>? imagePaths = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/ParkingSpaces");

            ResidentialAreas.API.EntityModels.ParkingSpace parkingSpace = new ResidentialAreas.API.EntityModels.ParkingSpace
            {
                Id = Guid.NewGuid(),
                AreaId = area.Id,
                Name = request.Name,
                Description = request.Description,
                BlockNo = request.BlockNo,
                Status = System.Enum.Parse<Status>(request.Status, true),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };


            try
            {
                await _areaDbContext.ParkingSpaces.AddAsync(parkingSpace, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding new parking space to the database.");
                return new AddNewParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "Database error",
                    StatusCode = 500,
                    Detail = "An error occurred while saving the parking space. Please try again later."
                });
            }

            

            if (imagePaths != null && imagePaths.Count > 0)
            {
                List<Image> images = imagePaths.Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    Url = path,
                    ImageType = ImageType.ParkingSpace,
                    ParkingSpaceCode = parkingSpace.ParkingSpaceCode
                }).ToList();

                try
                {
                    await _areaDbContext.Images.AddRangeAsync(images, cancellationToken);
                    await _areaDbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while saving parking space images to the database.");
                    return new AddNewParkingSpaceResult(null, new ErrorCarrier
                    {
                        Title = "Image upload error",
                        StatusCode = 500,
                        Detail = "An error occurred while saving the parking space images. Please upload image later."
                    });
                }

            }

            return new AddNewParkingSpaceResult(new AddNewParkingSpaceResponse
                (
                parkingSpace.Id,
                parkingSpace.ParkingSpaceCode,
                parkingSpace.Name ?? string.Empty,
                parkingSpace.Description,
                parkingSpace.BlockNo ?? string.Empty,
                parkingSpace.Status.ToString(),
                area.Code,
                area.Name,
                imagePaths),null);
        }
    }
}
