using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.UpdateBuildingByCode
{
    public record UpdateBuildingByCodeCommand(long Code, string Name, string BlockNo, int TotalFloors, string Address, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateBuildingByCodeResult>;
    public record UpdateBuildingByCodeResult(UpdateBuildingByCodeResponse? Result, ErrorCarrier? Error);

    public class UpdateBuildingByCodeHandler : ICommandHandler<UpdateBuildingByCodeCommand, UpdateBuildingByCodeResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateBuildingByCodeHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateBuildingByCodeHandler(AreaDbContext areaDbContext, ILogger<UpdateBuildingByCodeHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UpdateBuildingByCodeResult> Handle(UpdateBuildingByCodeCommand request, CancellationToken cancellationToken)
        {
            // Validate Building existence
            Building? building = await _areaDbContext.Buildings.AsNoTracking().Include(a=>a.Area).FirstOrDefaultAsync(b => b.Code == request.Code, cancellationToken);
            if (building == null)
            {
                _logger.LogWarning("Building with Code {Code} not found for update.", request.Code);
                return new UpdateBuildingByCodeResult(null, new ErrorCarrier {
                    Title = "BUILDING_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No building found with Code {request.Code}."
                });
            }



            // Authorization check: Only Admins, the Complex Manager of the area, or the Tenant of the building can update it
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if(!userRoles.Contains("Admin"))
            {
                bool isComplexManager = userRoles.Contains("ComplexManager");
                bool isTenant = userRoles.Contains("Tenant");

                // If the user is a tenant, check if they are the tenant of the building
                if (isTenant && (building.TenantId != null) && (building.TenantId != Guid.Parse(userIdClaim.Value)))
                {
                    return new UpdateBuildingByCodeResult(null, new ErrorCarrier {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to update this building."
                    });
                }

                // If the user is a complex manager, check if they are the complex manager of the area
                if (isComplexManager && (building.Area != null) && (building.Area.ComplexManagerId != null) && (building.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value)))
                {
                    return new UpdateBuildingByCodeResult(null, new ErrorCarrier {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to update this building."
                    });
                }

            }





            // Update building properties
            building.Name = request.Name;
            building.BlockNo = request.BlockNo;
            building.TotalFloors = request.TotalFloors;
            building.Address = request.Address;
            building.Status = (Status)System.Enum.Parse(typeof(Status), request.Status, true);
            building.UpdatedAt = DateTime.UtcNow;



            // Get existing image URLs for the building
            List<string?>? existingImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);



            // Process removed images: Extract the image paths from the URLs provided in the request
            List<string> removedImagePaths = new();
            if (request.RemovedImagesUrls != null)
            {
                var tasks = request.RemovedImagesUrls.Select(url => _imageSaver.GetPath(url!));
                var paths = await Task.WhenAll(tasks);
                removedImagePaths = paths.ToList();
            }



            // Image paths that need to be removed from the database and deleted from storage
            List<string?>? imagesToRemove = existingImageUrls.Where(url => removedImagePaths != null && removedImagePaths.Contains(url!)).ToList();



            //await _imageSaver.DeleteImages(imagesToRemove);



            // Begin a transaction to ensure atomicity of database operations and image deletions
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);


            // First, delete the images from storage. If this fails, we can rollback without affecting the database.
            try
            {
                await _areaDbContext.SaveChangesAsync(cancellationToken);
                await _areaDbContext.Images.Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building && imagesToRemove.Contains(i.Url)).ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to delete images for building with code {BuildingCode}", building.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateBuildingByCodeResult(null, new ErrorCarrier {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating. Please try again later."
                });
            }



            // Next, save the new images. If this fails, we can rollback the transaction to undo any database changes.
            List<string?>? imagePath = new List<string?>();
            try
            {
                imagePath = await _imageSaver.SaveImageAsync(request.AddedBase64StringImages, "wwwroot/images/buildings");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateBuildingByCodeResult(null, new ErrorCarrier {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while saving images. Please try again later."
                });
            }



            // Create new Image entities for the successfully saved images
            List<Image> newImages = imagePath.Select(path => new Image
            {
                Id = Guid.NewGuid(),
                Url = path,
                BuildingCode = building.Code,
                ImageType = ImageType.Building,
            }).ToList();



            // Update the building entity in the database
            try
            {
                await _areaDbContext.Images.AddRangeAsync(newImages, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to add new images for building with code {BuildingCode}", building.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateBuildingByCodeResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating. Please try again later."
                });
            }

            // If we reach this point, all operations have succeeded, so we can commit the transaction
            try
            {
                await transaction.CommitAsync(cancellationToken);

            }
            catch
            {
                _logger.LogError("Failed to commit transaction for building with code {BuildingCode}", building.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateBuildingByCodeResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating. Please try again later."
                });
            }


            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");



            // Construct the full image URLs for the response
            List<string>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building)
                .Select(i => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{i.Url}")
                .ToListAsync(cancellationToken);


            // Get area information for the response
            var areaInfo = await _areaDbContext.Areas.AsNoTracking()
                .Where(a => a.Id == building.AreaId)
                .Select(a => new { a.Code, a.Name })
                .FirstOrDefaultAsync(cancellationToken);


            // Return the updated building information along with the new list of image URLs
            return new UpdateBuildingByCodeResult(new UpdateBuildingByCodeResponse( building.Id!.Value, building.Code, building.Name ?? string.Empty, building.BlockNo ?? string.Empty, building.TotalFloors, building.Address ?? string.Empty, building.Status.ToString(), areaInfo?.Code ?? 0, areaInfo?.Name ?? string.Empty, allImageUrls!), null);
        }
    }
}