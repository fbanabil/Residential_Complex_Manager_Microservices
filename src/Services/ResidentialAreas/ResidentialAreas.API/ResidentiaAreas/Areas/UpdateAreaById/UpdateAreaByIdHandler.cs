using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.ResidentiaAreas.Areas.UpdateAreaByCode;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.UpdateAreaById
{

    public record UpdateAreaByIdCommand(Guid Id, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages) : ICommand<UpdateAreaByIdResult>;

    public record UpdateAreaByIdResult(Guid Id, long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? ImageUrls);


    public class UpdateAreaByIdHandler : ICommandHandler<UpdateAreaByIdCommand, UpdateAreaByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateAreaByIdHandler> _logger;
        private readonly IImageSaver _imageSaver;


        public UpdateAreaByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateAreaByIdHandler> logger, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
        }

        public async Task<UpdateAreaByIdResult> Handle(UpdateAreaByIdCommand request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Area with Id {Id} not found for update.", request.Id);
                
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

            List<string?>? existingImageUrls = await _areaDbContext.Images.Where(ai => ai.Code == area.Code && ai.ImageType == ImageType.Area).Select(ai => ai.Url).ToListAsync(cancellationToken);



            List<string?>? removedImagePaths = request.RemovedImagesUrls?.Select(url => url != null && url.Contains("images/") ? "images/" + url.Split("images/").LastOrDefault() : url).ToList();

            List<string?>? imagesToRemove = existingImageUrls.Where(url => removedImagePaths != null && removedImagePaths.Contains(url)).ToList();


            await _imageSaver.DeleteImages(imagesToRemove);
            await _areaDbContext.Images.Where(ai => ai.Code == area.Code && ai.ImageType == ImageType.Area && imagesToRemove.Contains(ai.Url)).ExecuteDeleteAsync(cancellationToken);



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
                    _areaDbContext.Images.Add(new Image { Code = area.Code, ImageType = ImageType.Area, Url = imagePath });
                }
            }

            await _areaDbContext.SaveChangesAsync(cancellationToken);

            List<string?>? allImageUrls = await _areaDbContext.Images.AsNoTracking().Where(ai => ai.Code == area.Code && ai.ImageType == ImageType.Area).Select(ai => ai.Url).ToListAsync(cancellationToken);

            return new UpdateAreaByIdResult(area.Id, area.Code, area.Name, area.City, area.State, area.Country, area.PostalCode, area.Address, area.GeoBoundary, area.Status.ToString(), allImageUrls);
        }
    }
}
