using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.UpdateParkingSpaceById
{
    public record UpdateParkingSpaceByIdCommand(Guid Id, long AreaCode, string Name, string? Description, string BlockNo, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateParkingSpaceByIdResult>;
    public record UpdateParkingSpaceByIdResult(UpdateParkingSpaceByIdResponse? Result, ErrorCarrier? ErrorCarrier);

    public class UpdateParkingSpaceByIdHandler : ICommandHandler<UpdateParkingSpaceByIdCommand, UpdateParkingSpaceByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateParkingSpaceByIdHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateParkingSpaceByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateParkingSpaceByIdHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UpdateParkingSpaceByIdResult> Handle(UpdateParkingSpaceByIdCommand request, CancellationToken cancellationToken)
        {
            // Validate request
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



            // Check for existing parking space with the same name in the same area
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




            // Validate area existence
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




            // Authorization check
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if(!userRoles.Contains("Admin"))
            {
                if((area.ComplexManagerId==null || area.ComplexManagerId != Guid.Parse(userIdClaim.Value)) && parkingSpace.AreaId != area.Id)
                {
                    _logger.LogWarning("Unauthorized update attempt by user {UserId} for parking space with Id {ParkingSpaceId}.", userIdClaim.Value, parkingSpace.Id);
                    
                    return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                    {
                        Title = "Unauthorized",
                        StatusCode = 403,
                        Detail = "You do not have permission to update this parking space."
                    });
                }
            }


            // Begin transaction
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);



            // Update parking space
            try
            {
                await _areaDbContext.ParkingSpaces.Where(p => p.Id == parkingSpace.Id)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(ps => ps.AreaId, area.Id)
                        .SetProperty(ps => ps.Name, request.Name)
                        .SetProperty(ps => ps.Description, request.Description)
                        .SetProperty(ps => ps.BlockNo, request.BlockNo)
                        .SetProperty(ps => ps.Status, (Status)System.Enum.Parse(typeof(Status), request.Status, true))
                        .SetProperty(ps => ps.UpdatedAt, DateTime.UtcNow), cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update parking space with Id {Id}", parkingSpace.Id);
                return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                {
                    Title = "Database update failed",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the parking space. Please try again later."
                });
            }



            // Get existing image URLs for the parking space
            List<string?>? existingImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.ParkingSpaceCode == parkingSpace.ParkingSpaceCode && i.ImageType == ImageType.ParkingSpace)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);




            // Determine which images to remove based on the provided URLs
            List<string?>? removedImagePaths = new List<string?>();
            if(request.RemovedImagesUrls != null)
            {
                var tasks = request.RemovedImagesUrls.Select(url=> _imageSaver.GetPath(url!)).ToList();
                var results = await Task.WhenAll(tasks);
                removedImagePaths = results.ToList()!;
            }


            // Determine which images to remove based on the provided URLs
            List<string?>? imagesToRemove = existingImageUrls
                .Where(url => removedImagePaths != null && removedImagePaths.Contains(url))
                .ToList();



            //await _imageSaver.DeleteImages(imagesToRemove);



            // Delete image records for removed images
            try
            {
                await _areaDbContext.Images
                .Where(i => i.ParkingSpaceCode == parkingSpace.ParkingSpaceCode && i.ImageType == ImageType.ParkingSpace && imagesToRemove.Contains(i.Url))
                .ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to delete image records for parking space with code {ParkingSpaceCode}", parkingSpace.ParkingSpaceCode);
                return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                {
                    Title = "Database update failed",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the parking space images. Please try again later."
                });
            }




            // Save new images and create image records
            List<string?>? imagePaths = new List<string?>();
            if(request.AddedBase64StringImages != null)
            {
                try
                { 
                    imagePaths = await _imageSaver.SaveImageAsync(request.AddedBase64StringImages, "wwwroot/images/ParkingSpaces");
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                    {
                        Title = "Image save failed",
                        StatusCode = 500,
                        Detail = "An error occurred while saving the parking space images. Please try again later."
                    });
                }
            }

            List<Image> newImageRecords = imagePaths.Select(path => new Image
            {
                Id = Guid.NewGuid(),
                ParkingSpaceCode = parkingSpace.ParkingSpaceCode,
                Url = path,
                ImageType = ImageType.ParkingSpace,
            }).ToList();




            // Add new image records to the database
            try
            {
                await _areaDbContext.Images.AddRangeAsync(newImageRecords, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update parking space with Id {Id}", parkingSpace.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                {
                    Title = "Database update failed",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the parking space. Please try again later."
                });
            }




            // Commit transaction
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit transaction for parking space update with Id {Id}", parkingSpace.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateParkingSpaceByIdResult(null, new ErrorCarrier
                {
                    Title = "Database update failed",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the parking space. Please try again later."
                });
            }

            // Retrieve updated parking space with area details and image URLs
            var httpContext = _httpContextAccessor.HttpContext;
            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.ParkingSpaceCode == parkingSpace.ParkingSpaceCode && i.ImageType == ImageType.ParkingSpace)
                .Select(i =>  $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{i.Url}")
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
