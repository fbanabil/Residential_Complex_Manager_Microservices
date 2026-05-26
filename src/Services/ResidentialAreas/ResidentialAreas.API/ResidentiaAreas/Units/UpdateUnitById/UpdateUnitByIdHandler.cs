using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.ResidentiaAreas.Units.AddNewUnit;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Units.UpdateUnitById
{
    public record UpdateUnitByIdCommand(Guid Id, long BuildingCode, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateUnitByIdResult>;

    public record UpdateUnitByIdResult(UpdateUnitByIdResponse? Result, ErrorCarrier? Error);

    public class UpdateUnitByIdHandler : ICommandHandler<UpdateUnitByIdCommand, UpdateUnitByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateUnitByIdHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateUnitByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateUnitByIdHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UpdateUnitByIdResult> Handle(UpdateUnitByIdCommand request, CancellationToken cancellationToken)
        {
            // Validate Unit existence
            EntityModels.Unit? unit = await _areaDbContext.Units.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
            if (unit == null)
            {
                _logger.LogWarning("Unit with Id {Id} not found for update.", request.Id);
                return new UpdateUnitByIdResult(null, new ErrorCarrier
                {
                    Title = "UNIT_NOT_FOUND",
                    StatusCode = StatusCodes.Status404NotFound,
                    Detail = $"Unit with Id {request.Id} not found."
                });
            }




            // Validate Building existence
            Building? building = await _areaDbContext.Buildings.AsNoTracking().Include(b => b.Area).FirstOrDefaultAsync(b => b.Code == request.BuildingCode, cancellationToken);
            if (building == null)
            {
                _logger.LogWarning("Building with code {BuildingCode} not found for unit update.", request.BuildingCode);
                return new UpdateUnitByIdResult(null, new ErrorCarrier
                {
                    Title = "BUILDING_NOT_FOUND",
                    StatusCode = StatusCodes.Status404NotFound,
                    Detail = $"Building with code {request.BuildingCode} not found."
                });
            }



            // Check for duplicate UnitNo within the same building
            bool duplicateUnitNo = await _areaDbContext.Units.AsNoTracking()
                .AnyAsync(u => u.Id != request.Id && u.BuildingId == building.Id && u.UnitNo == request.UnitNo, cancellationToken);
            if (duplicateUnitNo)
            {
                _logger.LogWarning("Unit number {UnitNo} already exists in building {BuildingCode}.", request.UnitNo, request.BuildingCode);
                return new UpdateUnitByIdResult(null, new ErrorCarrier
                {
                    Title = "DUPLICATE_UNIT_NO",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = $"Unit number {request.UnitNo} already exists in building with code {request.BuildingCode}."
                });
            }




            // Authorization check
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if (!userRoles.Contains("Admin"))
            {
                bool isComplexManager = userRoles.Contains("ComplexManager");
                bool isTenant = userRoles.Contains("Tenant");


                if (isComplexManager && (building.Area == null || building.Area?.ComplexManagerId == null || building.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value)))
                {
                    _logger.LogWarning("Complex manager with user ID {UserId} attempted to create a unit in building {BuildingCode} that they do not manage.", userIdClaim.Value, request.BuildingCode);
                    return new UpdateUnitByIdResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You do not have permission to update a unit in this building."
                    });
                }

                if (isTenant && (building.TenantId == null || building.TenantId != Guid.Parse(userIdClaim.Value)))
                {
                    _logger.LogWarning("Tenant with user ID {UserId} attempted to create a unit in building {BuildingCode} that they do not reside in.", userIdClaim.Value, request.BuildingCode);
                    return new UpdateUnitByIdResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You do not have permission to update a unit in this building."
                    });
                }
            }



            // Begin transaction
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);



            // Update Unit properties
            try
            {
                await _areaDbContext.Units
                    .Where(u => u.Id == request.Id)
                    .ExecuteUpdateAsync(
                    u => u.SetProperty(unit => unit.BuildingId, building.Id)
                          .SetProperty(unit => unit.UnitNo, request.UnitNo)
                          .SetProperty(unit => unit.FloorNo, request.FloorNo)
                          .SetProperty(unit => unit.UnitType, System.Enum.Parse<UnitType>(request.UnitType, true))
                          .SetProperty(unit => unit.Bedrooms, request.Bedrooms)
                          .SetProperty(unit => unit.Bathrooms, request.Bathrooms)
                          .SetProperty(unit => unit.AreaSqft, request.AreaSqft)
                          .SetProperty(unit => unit.OccupancyStatus, System.Enum.Parse<OccupancyStatus>(request.OccupancyStatus, true))
                          .SetProperty(unit => unit.OwnershipType, System.Enum.Parse<OwnershipType>(request.OwnershipType, true))
                          .SetProperty(unit => unit.CurrentLeaseId, request.CurrentLeaseId)
                          .SetProperty(unit => unit.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to update unit with Id {Id}. Rolling back transaction.", request.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateUnitByIdResult(null, new ErrorCarrier
                {
                    Title = "UNIT_UPDATE_FAILED",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = $"Failed to update unit with Id {request.Id}."
                });
            }




            // Get existing images for the unit
            List<string?>? existingImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.UnitCode == unit.Code && i.ImageType == ImageType.Unit)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);



            // Determine which images to remove based on the provided URLs
            List<string?>? removedImagePaths = new List<string?>();
            if(request.RemovedImagesUrls != null)
            {
                var tasks = request.RemovedImagesUrls.Select(url => _imageSaver.GetPath(url!));
                var result = await Task.WhenAll(tasks);
                removedImagePaths = result.ToList()!;
            }


            // Determine which images to remove based on the provided URLs
            List<string?>? imagesToRemove = existingImageUrls
                .Where(url => removedImagePaths != null && removedImagePaths.Contains(url))
                .ToList();

            //await _imageSaver.DeleteImages(imagesToRemove);


            // Delete images from the database
            try
            {
                await _areaDbContext.Images
                .Where(i => i.UnitCode == unit.Code && i.ImageType == ImageType.Unit && imagesToRemove.Contains(i.Url))
                .ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to delete images for unit with code {UnitCode}. Rolling back transaction.", unit.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateUnitByIdResult(null, new ErrorCarrier
                {
                    Title = "IMAGE_DELETE_FAILED",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = $"Failed to delete images for unit with code {unit.Code}."
                });
            }


            // Save new images and add their records to the database
            List<string?>? imagePaths = new List<string?>();
            if(request.AddedBase64StringImages != null)
            {
                imagePaths = await _imageSaver.SaveImageAsync(request.AddedBase64StringImages, "wwwroot/images/Units");
            }



            // Create new Image entities for the added images
            List<Image> newImages = imagePaths
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    UnitCode = unit.Code,
                    Url = path!,
                    ImageType = ImageType.Unit
                })
                .ToList();



            // Save new images to the database
            try
            {
                await _areaDbContext.Images.AddRangeAsync(newImages, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to save new images for unit with code {UnitCode}. Rolling back transaction.", unit.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateUnitByIdResult(null, new ErrorCarrier
                {
                    Title = "IMAGE_SAVE_FAILED",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = $"Failed to save new images for unit with code {unit.Code}."
                });
            }



            // Commit transaction
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to commit transaction for updating unit with Id {Id}. Rolling back transaction.", request.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateUnitByIdResult(null, new ErrorCarrier
                {
                    Title = "TRANSACTION_COMMIT_FAILED",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = $"Failed to commit transaction for updating unit with Id {request.Id}."
                });
            }



            // Retrieve the updated unit with building details and image URLs to return in the response
            var httpContext = _httpContextAccessor.HttpContext;

            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
            .Where(i => i.UnitCode == unit.Code && i.ImageType == ImageType.Unit)
            .Select(i => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{i.Url}")
            .ToListAsync(cancellationToken);



            _logger.LogInformation("Unit updated successfully with ID {Id}", request.Id);
            return new UpdateUnitByIdResult(new UpdateUnitByIdResponse(
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
                allImageUrls), null);
        }
    }
}
