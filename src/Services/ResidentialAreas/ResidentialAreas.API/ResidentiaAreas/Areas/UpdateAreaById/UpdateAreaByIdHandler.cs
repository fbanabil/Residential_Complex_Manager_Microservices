using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.UpdateAreaById
{
    public record UpdateAreaByIdCommand(Guid Id, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateAreaByIdResult>;

    public record UpdateAreaByIdResult(UpdateAreaByIdResponse? Result, ErrorCarrier? Error);

    public class UpdateAreaByIdHandler : ICommandHandler<UpdateAreaByIdCommand, UpdateAreaByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IImageSaver _imageSaver;
        private readonly ILogger<UpdateAreaByIdHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateAreaByIdHandler(AreaDbContext areaDbContext, IImageSaver imageSaver, ILogger<UpdateAreaByIdHandler> logger, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _imageSaver = imageSaver;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UpdateAreaByIdResult> Handle(UpdateAreaByIdCommand request, CancellationToken cancellationToken)
        {
            // Retrieve the area by ID and check if it exists
            Area? area = await _areaDbContext.Areas.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Update area by ID failed: no area found with ID {AreaId}", request.Id);
                return new UpdateAreaByIdResult(null, new ErrorCarrier()
                {
                    Title = "NOT FOUND",
                    Detail = $"Area with id {request.Id} not found.",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }



            // Authorization check: Only allow if user is Admin or Complex Manager of the area
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if (!userRoles.Contains("Admin") && area.ComplexManagerId.HasValue && area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
            {
                _logger.LogWarning("Update area by ID failed: user {UserId} is not authorized for area ID {AreaId}", userIdClaim.Value, request.Id);
                return new UpdateAreaByIdResult(null, new ErrorCarrier()
                {
                    Title = "UNAUTHORIZED",
                    Detail = "You do not have permission to update this area.",
                    StatusCode = StatusCodes.Status403Forbidden
                });
            }


            // Begin a transaction to ensure atomicity of the update operation
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync();

            area.Name = request.Name;
            area.City = request.City;
            area.State = request.State;
            area.Country = request.Country;
            area.PostalCode = request.PostalCode;
            area.Address = request.Address;
            area.GeoBoundary = request.GeoBoundary;
            area.Status = (Status)System.Enum.Parse(typeof(Status), request.Status, true);
            area.UpdatedAt = DateTime.UtcNow;



            // Save changes to the area entity
            try
            {
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Database update failed while updating area with id {AreaId}.", request.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByIdResult(null, new ErrorCarrier()
                {
                    Title = "DATABASE UPDATE FAILED",
                    Detail = "An error occurred while updating the area. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }



            // Retrieve existing image URLs for the area
            List<string?>? existingImageUrls = await _areaDbContext.Images.Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area).Select(ai => ai.Url).ToListAsync(cancellationToken);


            // Normalize the removed image URLs to match the stored URLs in the database
            List<string?>? removedImagePaths = request.RemovedImagesUrls?.Select(url => url != null && url.Contains("images/") ? "images/" + url.Split("images/").LastOrDefault() : url).ToList();


            // Determine which images need to be removed based on the provided URLs
            List<string?>? imagesToRemove = existingImageUrls.Where(url => removedImagePaths != null && removedImagePaths.Contains(url)).ToList();





            // Deletion of actual file will be done by background service periodically

            //await _imageSaver.DeleteImages(imagesToRemove);



            // Remove the images from the database and delete the corresponding files from the file system
            try
            {
                await _areaDbContext.Images.Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area && imagesToRemove.Contains(ai.Url)).ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Image deletion failed while updating area with id {AreaId}.", request.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByIdResult(null, new ErrorCarrier()
                {
                    Title = "IMAGE DELETION FAILED",
                    Detail = "Failed to delete one or more images. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }



            // Save the new images and get their paths
            List<string?>? imagePaths = new List<string?>();
            try
            {
                imagePaths = await _imageSaver.SaveImageAsync(request.AddedBase64StringImages, "wwwroot/images/areas");
            }
            catch
            {
                _logger.LogError("Image saving failed while updating area with id {AreaId}.", request.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByIdResult(null, new ErrorCarrier()
                {
                    Title = "IMAGE SAVING FAILED",
                    Detail = "An error occurred while saving one or more images. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }



            // Create new image records for the saved images and associate them with the area
            List<Image> dbImagesToSave = imagePaths.Select(im => new Image
            {
                Id = Guid.NewGuid(),
                AreaCode = area.Code,
                ImageType = ImageType.Area,
                Url = im
            }).ToList();



            // Save the new image records to the database
            try
            {
                await _areaDbContext.Images.AddRangeAsync(dbImagesToSave, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Database update failed while saving images for area with id {AreaId}.", request.Id);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByIdResult(null, new ErrorCarrier()
                {
                    Title = "DATABASE UPDATE FAILED",
                    Detail = "An error occurred while saving images for the area. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }



            // Commit the transaction after all operations have succeeded
            await transaction.CommitAsync(cancellationToken);



            // Retrieve all image URLs for the area to include in the response
            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is not available.");
            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking().Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area).Select(ai => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{ai.Url}").ToListAsync(cancellationToken);


            _logger.LogInformation("Area updated successfully with ID {AreaId}", request.Id);
            // Return the updated area details along with the image URLs in the response
            return new UpdateAreaByIdResult(new UpdateAreaByIdResponse(area.Id, area.Code, area.Name, area.City, area.State, area.Country, area.PostalCode, area.Address, area.GeoBoundary, area.Status.ToString(), allImageUrls), null);
        }
    }
}
