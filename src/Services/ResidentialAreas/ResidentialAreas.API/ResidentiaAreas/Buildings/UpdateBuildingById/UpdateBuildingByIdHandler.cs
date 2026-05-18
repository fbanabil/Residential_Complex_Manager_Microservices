using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.UpdateBuildingById
{
    public record UpdateBuildingByIdCommand(Guid Id, string Name, string BlockNo, int TotalFloors, string Address, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateBuildingByIdResult>;
    public record UpdateBuildingByIdResult(UpdateBuildingByIdResponse? Result, ErrorCarrier? Error);

    public class UpdateBuildingByIdHandler : ICommandHandler<UpdateBuildingByIdCommand, UpdateBuildingByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateBuildingByIdHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateBuildingByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateBuildingByIdHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UpdateBuildingByIdResult> Handle(UpdateBuildingByIdCommand request, CancellationToken cancellationToken)
        {
            // Validate Building existence
            Building? building = await _areaDbContext.Buildings.AsNoTracking().Include(a => a.Area).FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);
            if (building == null)
            {
                _logger.LogWarning("Building with Id {Id} not found for update.", request.Id);
                return new UpdateBuildingByIdResult(null, new ErrorCarrier
                {
                    Title = "BUILDING_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No building found with Id {request.Id}."
                });
            }




            // Authorization check: Only Admins, the Complex Manager of the area, or the Tenant of the building can update it
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if (!userRoles.Contains("Admin"))
            {
                bool isComplexManager = userRoles.Contains("ComplexManager");
                bool isTenant = userRoles.Contains("Tenant");

                // If the user is a tenant, they can only update the building if they are the tenant of that building
                if (isTenant && (building.TenantId != null) && (building.TenantId != Guid.Parse(userIdClaim.Value)))
                {
                    return new UpdateBuildingByIdResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to update this building."
                    });
                }


                // If the user is a complex manager, they can only update the building if they are the complex manager of the area that the building belongs to
                if (isComplexManager && (building.Area != null) && (building.Area.ComplexManagerId != null) && (building.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value)))
                {
                    return new UpdateBuildingByIdResult(null, new ErrorCarrier
                    {
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



            // All images existing in the database for the building
            List<string?>? existingImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);



            // Determine which images to remove based on the URLs provided in the request
            List<string> removedImagePaths = new();
            if (request.RemovedImagesUrls != null)
            {
                var tasks = request.RemovedImagesUrls.Select(url => _imageSaver.GetPath(url!));
                var paths = await Task.WhenAll(tasks);
                removedImagePaths = paths.ToList();
            }



            // Identify the images that need to be removed from the database based on the URLs provided in the request
            List<string?>? imagesToRemove = existingImageUrls.Where(url => removedImagePaths != null && removedImagePaths.Contains(url!)).ToList();


            // Begin a transaction to ensure atomicity of the update operation
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);




            // First, update the building entity and delete the images that are marked for removal
            try
            {
                await _areaDbContext.SaveChangesAsync(cancellationToken);
                await _areaDbContext.Images.Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building && imagesToRemove.Contains(i.Url)).ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to delete images for building with id {BuildingId}", building.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateBuildingByIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating. Please try again later."
                });
            }



            // Save the new images provided in the request
            List<string?>? imagePath = new List<string?>();
            try
            {
                imagePath = await _imageSaver.SaveImageAsync(request.AddedBase64StringImages, "wwwroot/images/buildings");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateBuildingByIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while saving images. Please try again later."
                });
            }




            // Create new Image entities for the newly added images and associate them with the building
            List<Image> newImages = imagePath.Select(path => new Image
            {
                Id = Guid.NewGuid(),
                Url = path,
                BuildingCode = building.Code,
                ImageType = ImageType.Building,
            }).ToList();



            /// Add the new images to the database
            try
            {
                await _areaDbContext.Images.AddRangeAsync(newImages, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to add new images for building with id {BuildingId}", building.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateBuildingByIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating. Please try again later."
                });
            }




            // Commit the transaction if all operations succeeded
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to commit transaction for building with id {BuildingId}", building.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateBuildingByIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating. Please try again later."
                });
            }


            // Get http context to build the full URLs for the images
            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");



            // Build the full URLs for all images associated with the building, including both existing and newly added images
            List<string>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.BuildingCode == building.Code && i.ImageType == ImageType.Building)
                .Select(i => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{i.Url}")
                .ToListAsync(cancellationToken);


            // Retrieve the area information to include in the response
            var areaInfo = await _areaDbContext.Areas.AsNoTracking()
                .Where(a => a.Id == building.AreaId)
                .Select(a => new { a.Code, a.Name })
                .FirstOrDefaultAsync(cancellationToken);


            // Return the updated building information in the response
            return new UpdateBuildingByIdResult(new UpdateBuildingByIdResponse(building.Id!.Value, building.Code, building.Name ?? string.Empty, building.BlockNo ?? string.Empty, building.TotalFloors, building.Address ?? string.Empty, building.Status.ToString(), areaInfo?.Code ?? 0, areaInfo?.Name ?? string.Empty, allImageUrls!), null);
        }
    }
}
