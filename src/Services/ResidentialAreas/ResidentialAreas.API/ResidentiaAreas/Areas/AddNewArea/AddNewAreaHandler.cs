using ResidentialAreas.API.Helpers.ImageSaver;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea
{
    public record AddNewAreaCommand(string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, string ImageBase64) : ICommand<AddNewAreaResult>;

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
            string imagePath= string.Empty;
            try
            {
                imagePath = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/areas");
            }
            catch (Exception ex)
            {
                imagePath = "images/default.jpg";
                Console.WriteLine($"Error saving image: {ex.Message}");
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
            Image areaImage = new Image
            {
                Id = Guid.NewGuid(),
                ImageType = ImageType.Area,
                Url = imagePath,
                Code = newArea.Code
            };

            _areaDbContext.Images.Add(areaImage);
            await _areaDbContext.SaveChangesAsync(cancellationToken);

            return new AddNewAreaResult(newArea.Id, newArea.Name, newArea.Code);
        }
    }
}
