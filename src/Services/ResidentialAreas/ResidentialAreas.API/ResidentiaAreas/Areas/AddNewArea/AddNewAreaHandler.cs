using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea
{
    public record AddNewAreaCommand(string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? ImageBase64) : ICommand<AddNewAreaResult>;

    public record AddNewAreaResult(Guid Id, string Name, long Code);
    public class AddNewAreaHandler : ICommandHandler<AddNewAreaCommand, AddNewAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IImageSaver _imageSaver;
        public AddNewAreaHandler(AreaDbContext areaDbContext, IImageSaver imageSaver)
        {
            _areaDbContext = areaDbContext;
            _imageSaver = imageSaver;
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
                        Console.WriteLine($"Error saving image: {ex.Message}");
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

            _areaDbContext.Areas.Add(newArea);
            await _areaDbContext.SaveChangesAsync(cancellationToken);


            List<Image> areaImages = new List<Image>();
            areaImages = imagePathToAdd.Select(imgPath => new Image
            {
                Id = Guid.NewGuid(),
                ImageType = ImageType.Area,
                Url = imgPath,
                AreaCode = newArea.Code
            }).ToList();

            _areaDbContext.Images.AddRange(areaImages);
            await _areaDbContext.SaveChangesAsync(cancellationToken);

            return new AddNewAreaResult(newArea.Id, newArea.Name, newArea.Code);
        }
    }
}
