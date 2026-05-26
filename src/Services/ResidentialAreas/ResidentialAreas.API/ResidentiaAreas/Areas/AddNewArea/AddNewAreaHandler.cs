using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea
{
    public record AddNewAreaCommand(string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? ImageBase64) : ICommand<AddNewAreaResult>;

    public record AddNewAreaResult(Guid? Id, string? Name, long? Code, string? ErrorMessage);
    public class AddNewAreaHandler : ICommandHandler<AddNewAreaCommand, AddNewAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IImageSaver _imageSaver;
        private readonly ILogger<AddNewAreaHandler> _logger;

        public AddNewAreaHandler(AreaDbContext areaDbContext, IImageSaver imageSaver, ILogger<AddNewAreaHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _imageSaver = imageSaver;
            _logger = logger;
        }

        public async Task<AddNewAreaResult> Handle(AddNewAreaCommand request, CancellationToken cancellationToken)
        {
            List<string?>? imageBase64List = request.ImageBase64;
            string imagePath = string.Empty;

            List<string?>? imagePathToAdd = new List<string?>();

            if (imageBase64List != null)
            {
                foreach (var imageBase64 in imageBase64List)
                {
                    if (string.IsNullOrEmpty(imageBase64))
                    {
                        continue;
                    }

                    try
                    {
                        imagePath = await _imageSaver.SaveImageAsync(imageBase64, "wwwroot/images/areas");
                    }
                    catch (Exception ex)
                    {
                        imagePath = "images/default.jpg";
                        _logger.LogError(ex, "Error saving image for area '{AreaName}', using default image", request.Name);
                    }

                    imagePathToAdd.Add(imagePath);

                }
            }



            Area newArea = new Area
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                City = request.City,
                State = request.State,
                Country = request.Country,
                PostalCode = request.PostalCode,
                Address = request.Address,
                GeoBoundary = request.GeoBoundary,
                Status = (Status)System.Enum.Parse(typeof(Status), request.Status, true),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };


            try
            {
                _areaDbContext.Areas.Add(newArea);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Add new area failed: database error while saving area '{AreaName}'", request.Name);
                return new AddNewAreaResult(null, null, null, $"Error saving area: Please try again later");
            }




            List<Image> areaImages = new List<Image>();
            areaImages = imagePathToAdd.Select(imgPath => new Image
            {
                Id = Guid.NewGuid(),
                ImageType = ImageType.Area,
                Url = imgPath,
                AreaCode = newArea.Code
            }).ToList();


            try
            {
                _areaDbContext.Images.AddRange(areaImages);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Add new area failed: database error while saving images for area '{AreaName}' with ID {AreaId}", request.Name, newArea.Id);
                return new AddNewAreaResult(newArea.Id, newArea.Name, newArea.Code, $"Error saving images: Please try again later");
            }

            _logger.LogInformation("Area '{AreaName}' created successfully with ID {AreaId} and code {AreaCode}", newArea.Name, newArea.Id, newArea.Code);
            return new AddNewAreaResult(newArea.Id, newArea.Name, newArea.Code, null);
        }
    }
}
