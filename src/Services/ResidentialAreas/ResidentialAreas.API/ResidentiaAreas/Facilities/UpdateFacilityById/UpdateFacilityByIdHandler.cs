using Microsoft.AspNetCore.Http;
using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.UpdateFacilityById
{
    public record UpdateFacilityByIdCommand(Guid Id, long? AreaCode, long? BuildingCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateFacilityByIdResult>;
    public record UpdateFacilityByIdResult(UpdateFacilityByIdResponse? Result, ErrorCarrier? Error);

    public class UpdateFacilityByIdHandler : ICommandHandler<UpdateFacilityByIdCommand, UpdateFacilityByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateFacilityByIdHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateFacilityByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateFacilityByIdHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UpdateFacilityByIdResult> Handle(UpdateFacilityByIdCommand request, CancellationToken cancellationToken)
        {
            // Validation: Check if facility exists
            Facility? facility = await _areaDbContext.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);
            if (facility == null)
            {
                _logger.LogWarning("Facility with Id {Id} not found for update.", request.Id);
                return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                {
                    Title = "BAD_REQUEST",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = "Facility with given id doesn't exist"
                });
            }




            // Validation: Check if both AreaCode and BuildingCode are provided
            if (request.AreaCode.HasValue && request.BuildingCode.HasValue)
            {
                _logger.LogWarning("Update facility failed. Both AreaCode and BuildingCode were provided for facility {Id}.", request.Id);
                return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                {
                    Title = "BAD_REQUEST",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = "Both AreaCode and BuildingCode cannot be provided at the same time. Please provide only one of them."
                });
            }




            // Validation: Check if at least one of AreaCode or BuildingCode is provided
            Area? area = null;
            Building? building = null;
            if (request.AreaCode.HasValue)
            {
                area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode.Value, cancellationToken);
                if (area == null)
                {
                    _logger.LogWarning("Area with code {AreaCode} not found for facility update.", request.AreaCode.Value);
                    return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                    {
                        Title = "BAD_REQUEST",
                        StatusCode = StatusCodes.Status400BadRequest,
                        Detail = "Area with given code doesn't exist"
                    });
                }
            }  
            else if (request.BuildingCode.HasValue)
            {
                building = await _areaDbContext.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Code == request.BuildingCode.Value, cancellationToken);
                if (building == null)
                {
                    _logger.LogWarning("Building with code {BuildingCode} not found for facility update.", request.BuildingCode.Value);
                    return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                    {
                        Title = "BAD_REQUEST",
                        StatusCode = StatusCodes.Status400BadRequest,
                        Detail = "Building with given code doesn't exist"
                    });
                }
            }




            // Authorization: Check if user has permission to update the facility
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if (!userRoles.Contains("Admin"))
            {
                if (userRoles.Contains("Tenant") && (building!.TenantId == null || building.TenantId != Guid.Parse(userIdClaim.Value)))
                {
                    _logger.LogWarning("Update facility failed: tenant {UserId} is not authorized for facility ID {FacilityId}", userIdClaim.Value, request.Id);
                    return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You do not have permission to update this facility."
                    });
                }

                if(userRoles.Contains("ComplexManager") && (area!.ComplexManagerId == null || area.ComplexManagerId != Guid.Parse(userIdClaim.Value)))
                {
                    _logger.LogWarning("Update facility failed: complex manager {UserId} is not authorized for facility ID {FacilityId}", userIdClaim.Value, request.Id);
                    return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You do not have permission to update this facility."
                    });
                }
            }




            // Start transaction
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);



            // Update facility details
            try
            {
                await _areaDbContext.Facilities
                .Where(f => f.Id == request.Id)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(a => a.AreaId, area != null ? area.Id : null)
                    .SetProperty(a => a.BuildingId, building != null ? building.Id : null)
                    .SetProperty(a => a.Name, request.Name)
                    .SetProperty(a => a.FacilityType, request.FacilityType)
                    .SetProperty(a => a.Capacity, request.Capacity)
                    .SetProperty(a => a.BookingRequired, request.BookingRequired)
                    .SetProperty(a => a.HourlyRate, request.HourlyRate)
                    .SetProperty(a => a.Rules, request.Rules)
                    .SetProperty(a => a.Status, System.Enum.Parse<Status>(request.Status, true))
                    .SetProperty(a => a.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to update facility properties for facility ID {FacilityId}", request.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while updating the facility. Please try again later."
                });
            }
            
         


            long facilityCode = facility.FacilityCode ?? 0;


            // Get existing image URLs for the facility
            List<string?>? existingImageUrls = await _areaDbContext.Images
                .Where(i => i.FacilityCode == facilityCode && i.ImageType == ImageType.Facility)
                .Select(i => i.Url)
                .ToListAsync(cancellationToken);


            // Determine which images to remove based on the provided URLs
            List<string>? removedImagePaths = null;
            if (request.RemovedImagesUrls != null)
            {
                var tasks = request.RemovedImagesUrls
                    .Where(url => url != null)
                    .Select(url => _imageSaver.GetPath(url!));
                var paths = await Task.WhenAll(tasks);
                removedImagePaths = paths.ToList();
            }


            // Filter existing image URLs to find which ones need to be removed
            List<string?>? imagesToRemove = existingImageUrls?
                .Where(url => removedImagePaths != null && removedImagePaths.Contains(url!))
                .ToList();



            //await _imageSaver.DeleteImages(imagesToRemove);


            // Remove images from the database that are marked for deletion
            try
            {
                await _areaDbContext.Images
                .Where(i => i.FacilityCode == facilityCode && i.ImageType == ImageType.Facility && imagesToRemove.Contains(i.Url))
                .ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to delete images for facility code {FacilityCode}", facilityCode);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while removing images for the facility. Please try again later."
                });
            }




            // Save new images and get their paths
            List<string?>? imagePaths = new List<string?>();
            try
            {
                imagePaths = await _imageSaver.SaveImageAsync(request.AddedBase64StringImages ?? [], "wwwroot/images/Facilities");
            }
            catch
            {
                _logger.LogError("Failed to save new images for facility code {FacilityCode}", facilityCode);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "Something went wrong."
                });
            }



            // Create Image entities for the new images and save them to the database
            List<Image> imagesToSave = imagePaths != null ? imagePaths.Select(x => new Image
            {
                Id = Guid.NewGuid(),
                FacilityCode = facilityCode,
                ImageType = ImageType.Facility,
                Url = x
            }).ToList() : [];




            // Save the new images to the database
            try
            {
                await _areaDbContext.Images.AddRangeAsync(imagesToSave,cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to save image records for facility code {FacilityCode}", facilityCode);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "Something went wrong."
                });
            }


            // Commit the transaction
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to commit transaction for facility update with ID {FacilityId}", request.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateFacilityByIdResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "Something went wrong."
                });
            }


            
            var httpContext = _httpContextAccessor.HttpContext;


            // Get all image URLs for the facility after the update
            List<string>? allImageUrls = await _areaDbContext.Images.AsNoTracking()
                .Where(i => i.FacilityCode == facilityCode && i.ImageType == ImageType.Facility)
                .Select(i => $"{httpContext!.Request.Scheme}://{httpContext!.Request.Host}/{i.Url}")
                .ToListAsync(cancellationToken);


            // Fetch the updated facility details to return in the response
            area ??= await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == facility.AreaId, cancellationToken);
            building ??= await _areaDbContext.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == facility.BuildingId, cancellationToken);



            _logger.LogInformation("Facility updated successfully with ID {FacilityId}", request.Id);
            return new UpdateFacilityByIdResult(new UpdateFacilityByIdResponse(
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
                allImageUrls), null);
        }
    }
}
