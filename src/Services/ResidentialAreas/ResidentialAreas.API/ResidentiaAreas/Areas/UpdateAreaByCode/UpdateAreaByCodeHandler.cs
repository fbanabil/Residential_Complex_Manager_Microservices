using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;
using System;
using System.Diagnostics;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.UpdateAreaByCode
{
    public record UpdateAreaByCodeCommand(long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateAreaByCodeResult>;

    public record UpdateAreaByCodeResult(Guid Id,long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? ImageUrls);


    public class UpdateAreaByCodeHandler : ICommandHandler<UpdateAreaByCodeCommand, UpdateAreaByCodeResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IImageSaver _imageSaver;
        private readonly ILogger<UpdateAreaByCodeHandler> _logger;
        public UpdateAreaByCodeHandler(AreaDbContext areaDbContext, IImageSaver imageSaver, ILogger<UpdateAreaByCodeHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _imageSaver = imageSaver;
            _logger = logger;
        }

        public async Task<UpdateAreaByCodeResult> Handle(UpdateAreaByCodeCommand request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.FirstOrDefaultAsync(a => a.Code == request.Code, cancellationToken);

            if (area == null)
            {
                return null;
            }


            area.Name = request.Name;
            area.City = request.City;
            area.State = request.State;
            area.Country = request.Country;
            area.PostalCode = request.PostalCode;
            area.Address = request.Address;
            area.GeoBoundary = request.GeoBoundary;
            area.Status = (Status)System.Enum.Parse(typeof(Status), request.Status, true);
            area.UpdatedAt = DateTime.UtcNow;
            await _areaDbContext.SaveChangesAsync(cancellationToken);



            List<string?>? existingImageUrls = await _areaDbContext.Images.Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area).Select(ai => ai.Url).ToListAsync(cancellationToken);


            List<string?>? removedImagePaths = request.RemovedImagesUrls?.Select(url => url != null && url.Contains("images/") ? "images/" + url.Split("images/").LastOrDefault() : url).ToList();

            List<string?>? imagesToRemove = existingImageUrls.Where(url => removedImagePaths != null && removedImagePaths.Contains(url)).ToList();


            await _imageSaver.DeleteImages(imagesToRemove);
            await _areaDbContext.Images.Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area && imagesToRemove.Contains(ai.Url)).ExecuteDeleteAsync(cancellationToken);



            string imagePath = String.Empty;

            foreach (string? base64Image in request.AddedBase64StringImages ?? [])
            {
                if (!string.IsNullOrEmpty(base64Image))
                {
                    try
                    {
                        imagePath = await _imageSaver.SaveImageAsync(base64Image, "wwwroot/images/areas");
                    }
                    catch
                    {
                        _logger.LogError("Failed to save image for area with code {AreaCode}", area.Code);
                        imagePath = "images/default.jpg";
                    }
                    _areaDbContext.Images.Add(new Image { AreaCode = area.Code, ImageType = ImageType.Area, Url = imagePath });
                }
            }

            await _areaDbContext.SaveChangesAsync(cancellationToken);

            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking().Where(ai => ai.AreaCode == area.Code && ai.ImageType == ImageType.Area).Select(ai => ai.Url).ToListAsync(cancellationToken);

            return new UpdateAreaByCodeResult(area.Id, area.Code, area.Name, area.City, area.State, area.Country, area.PostalCode, area.Address, area.GeoBoundary, area.Status.ToString(), allImageUrls);
        }
    }
}
