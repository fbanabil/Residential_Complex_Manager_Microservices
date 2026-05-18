using Microsoft.AspNetCore.Http;
using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;
using System;
using System.Diagnostics;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.UpdateAreaByCode
{
    public record UpdateAreaByCodeCommand(long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateAreaByCodeResult>;

    public record UpdateAreaByCodeResult(UpdateAreaByCodeResponse? Result, ErrorCarrier? Error);


    public class UpdateAreaByCodeHandler : ICommandHandler<UpdateAreaByCodeCommand, UpdateAreaByCodeResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IImageSaver _imageSaver;
        private readonly ILogger<UpdateAreaByCodeHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UpdateAreaByCodeHandler(AreaDbContext areaDbContext, IImageSaver imageSaver, ILogger<UpdateAreaByCodeHandler> logger, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _imageSaver = imageSaver;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UpdateAreaByCodeResult> Handle(UpdateAreaByCodeCommand request, CancellationToken cancellationToken)
        {
            // Retrieve the area by code
            Area? area = await _areaDbContext.Areas.FirstOrDefaultAsync(a => a.Code == request.Code, cancellationToken);
            if (area == null)
            {
                return new UpdateAreaByCodeResult(null, new ErrorCarrier()
                {
                    Title = "NOT FOUND",
                    Detail = $"Area with code {request.Code} not found.",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }



            // Authorization check: Only Admins or the Complex Manager of the area can update it
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if(!userRoles.Contains("Admin") && area.ComplexManagerId.HasValue && area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
            {
                return new UpdateAreaByCodeResult(null, new ErrorCarrier()
                {
                    Title = "UNAUTHORIZED",
                    Detail = "You do not have permission to update this area.",
                    StatusCode = StatusCodes.Status403Forbidden
                });
            }



            // Starting transaction
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync();



            // Update area properties
            area.Name = request.Name;
            area.City = request.City;
            area.State = request.State;
            area.Country = request.Country;
            area.PostalCode = request.PostalCode;
            area.Address = request.Address;
            area.GeoBoundary = request.GeoBoundary;
            area.Status = (Status)System.Enum.Parse(typeof(Status), request.Status, true);
            area.UpdatedAt = DateTime.UtcNow;



            // Save changes to area
            try
            {
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch 
            {
                _logger.LogError("Database update failed while updating area with code {AreaCode}.", request.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByCodeResult(null, new ErrorCarrier()
                {
                    Title = "DATABASE UPDATE FAILED",
                    Detail = "An error occurred while updating the area. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }



            // Handle image removals
            List<string?>? existingImageUrls = await _areaDbContext.Images.Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area).Select(ai => ai.Url).ToListAsync(cancellationToken);




            // Normalize removed image URLs to match stored URLs (e.g., "images/filename.jpg")
            List<string?>? removedImagePaths = request.RemovedImagesUrls?.Select(url => url != null && url.Contains("images/") ? "images/" + url.Split("images/").LastOrDefault() : url).ToList();




            // Determine which images to remove based on the normalized URLs
            List<string?>? imagesToRemove = existingImageUrls.Where(url => removedImagePaths != null && removedImagePaths.Contains(url)).ToList();




            // Deletion of actual file will be done by background service periodically


            //await _imageSaver.DeleteImages(imagesToRemove);





            // Delete the images from storage and database
            try
            {
                await _areaDbContext.Images.Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area && imagesToRemove.Contains(ai.Url)).ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {

                // Rollback if error happens
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByCodeResult(null, new ErrorCarrier()
                {
                    Title = "IMAGE DELETION FAILED",
                    Detail = "Failed to delete one or more images. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }



            // Save images that added
            List<string?>? imagePaths = new List<string?>();
            try
            {
                imagePaths = await _imageSaver.SaveImageAsync(request.AddedBase64StringImages, "wwwroot/images/areas");
            }
            catch
            {
                _logger.LogError("Image saving failed while updating area with code {AreaCode}.", request.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByCodeResult(null, new ErrorCarrier()
                {
                    Title = "IMAGE SAVING FAILED",
                    Detail = "An error occurred while saving one or more images. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }




            // Create image records to be saved in database
            List<Image> dbImagesToSave = imagePaths.Select(im => new Image
            {
                Id = Guid.NewGuid(),
                AreaCode = area.Code,
                ImageType = ImageType.Area,
                Url = im
            }).ToList();




            // Save the new images to database
            try
            {
                await _areaDbContext.Images.AddRangeAsync(dbImagesToSave, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Database update failed while saving images for area with code {AreaCode}.", request.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByCodeResult(null, new ErrorCarrier()
                {
                    Title = "DATABASE UPDATE FAILED",
                    Detail = "An error occurred while saving images for the area. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }



            // Commit the transaction if everything succeeded
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Transaction commit failed while updating area with code {AreaCode}.", request.Code);
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateAreaByCodeResult(null, new ErrorCarrier()
                {
                    Title = "TRANSACTION COMMIT FAILED",
                    Detail = "An error occurred while finalizing the update. Please try again.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }




            // Retrieve all image URLs for the area to return in the response
            var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is not available.");
            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking().Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area).Select(ai => $"{httpContext!.Request.Scheme}://{httpContext!.Request.Host}/{ai.Url}").ToListAsync(cancellationToken);



            // Return the updated area details in the response
            return new UpdateAreaByCodeResult(new UpdateAreaByCodeResponse(area.Id, area.Code, area.Name, area.City, area.State, area.Country, area.PostalCode, area.Address, area.GeoBoundary, area.Status.ToString(), allImageUrls), null);
        }
    }
}
