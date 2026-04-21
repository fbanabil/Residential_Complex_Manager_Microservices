using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.UpdateParkingSpaceById
{
    public record UpdateParkingSpaceByIdCommand(Guid Id, long AreaCode, string Name, string? Description, string BlockNo, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateParkingSpaceByIdResult>;
    public record UpdateParkingSpaceByIdResult(UpdateParkingSpaceByIdResponse? Result, ErrorCarrier? ErrorCarrier);

    public class UpdateParkingSpaceByIdHandler : ICommandHandler<UpdateParkingSpaceByIdCommand, UpdateParkingSpaceByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateParkingSpaceByIdHandler> _logger;
        private readonly IImageSaver _imageSaver;

        public UpdateParkingSpaceByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateParkingSpaceByIdHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<UpdateParkingSpaceByIdResult> Handle(UpdateParkingSpaceByIdCommand request, CancellationToken cancellationToken)
        {
            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
            if (parkingSpace == null)
            {
                _logger.LogWarning("Parking space with Id {Id} not found for update.", request.Id);
                
                return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                {
                    Title = "Parking space not found",
                    StatusCode = 404,
                    Detail = $"Parking space with Id {request.Id} not found."
                });

            }
                    


            bool existingWithSameName = await _areaDbContext.ParkingSpaces.AsNoTracking()
                .AnyAsync(p => p.Name == request.Name && p.AreaId == parkingSpace.AreaId, cancellationToken);

            if (existingWithSameName && request.Name != parkingSpace.Name)
            {
                _logger.LogWarning("Parking space with name {Name} already exists in area with Id {AreaId}.", request.Name, parkingSpace.AreaId);
                
                return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                {
                    Title = "Duplicate parking space name",
                    StatusCode = 400,
                    Detail = $"A parking space with the name {request.Name} already exists in the same area."
                });
            }





            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Area with code {AreaCode} not found for parking space update.", request.AreaCode);
                
                return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                {
                    Title = "Area not found",
                    StatusCode = 404,
                    Detail = $"Area with code {request.AreaCode} not found."
                });
            }



            parkingSpace.AreaId = area.Id;
            parkingSpace.Name = request.Name;
            parkingSpace.Description = request.Description;
            parkingSpace.BlockNo = request.BlockNo;
            parkingSpace.Status = (Status)System.Enum.Parse(typeof(Status), request.Status, true);
            parkingSpace.UpdatedAt = DateTime.UtcNow;


            List<string?>? existingImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.ParkingSpaceCode == parkingSpace.ParkingSpaceCode && i.ImageType == ImageType.ParkingSpace)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);

            List<string?>? removedImagePaths = request.RemovedImagesUrls?
                .Select(url => url != null && url.Contains("images/") ? "images/" + url.Split("images/").LastOrDefault() : url)
                .ToList();

            List<string?>? imagesToRemove = existingImageUrls
                .Where(url => removedImagePaths != null && removedImagePaths.Contains(url))
                .ToList();

            await _imageSaver.DeleteImages(imagesToRemove);


            try
            {
                await _areaDbContext.Images
                .Where(i => i.ParkingSpaceCode == parkingSpace.ParkingSpaceCode && i.ImageType == ImageType.ParkingSpace && imagesToRemove.Contains(i.Url))
                .ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to delete image records for parking space with code {ParkingSpaceCode}", parkingSpace.ParkingSpaceCode);
            }



            string imagePath = string.Empty;
            foreach (string? base64Image in request.AddedBase64StringImages ?? [])
            {
                if (!string.IsNullOrEmpty(base64Image))
                {
                    try
                    {
                        imagePath = await _imageSaver.SaveImageAsync(base64Image, "wwwroot/images/ParkingSpaces");
                    }
                    catch
                    {
                        _logger.LogError("Failed to save image for parking space with code {ParkingSpaceCode}", parkingSpace.ParkingSpaceCode);
                        imagePath = "images/default.jpg";
                    }

                    await _areaDbContext.Images.AddAsync(new Image
                    {
                        Id = Guid.NewGuid(),
                        ParkingSpaceCode = parkingSpace.ParkingSpaceCode,
                        ImageType = ImageType.ParkingSpace,
                        Url = imagePath
                    });
                }
            }
            
            try
            {
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update parking space with Id {Id}", parkingSpace.Id);
                
                return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                {
                    Title = "Database update failed",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the parking space. Please try again later."
                });
            }


            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.ParkingSpaceCode == parkingSpace.ParkingSpaceCode && i.ImageType == ImageType.ParkingSpace)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);

            return new UpdateParkingSpaceByIdResult(new UpdateParkingSpaceByIdResponse(
                parkingSpace.Id,
                parkingSpace.ParkingSpaceCode,
                parkingSpace.Name ?? string.Empty,
                parkingSpace.Description,
                parkingSpace.BlockNo ?? string.Empty,
                parkingSpace.Status.ToString(),
                area.Code,
                area.Name,
                allImageUrls),null);
        }
    }
}
