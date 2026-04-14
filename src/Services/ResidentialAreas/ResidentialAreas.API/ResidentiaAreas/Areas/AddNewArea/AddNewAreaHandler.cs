namespace ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea
{
    public record AddNewAreaCommand(string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status) : ICommand<AddNewAreaResult>;

    public record AddNewAreaResult(Guid Id, string Name, long Code);
    public class AddNewAreaHandler : ICommandHandler<AddNewAreaCommand, AddNewAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;
        public AddNewAreaHandler(AreaDbContext areaDbContext)
        {
            _areaDbContext = areaDbContext;
        }
        public async Task<AddNewAreaResult> Handle(AddNewAreaCommand request, CancellationToken cancellationToken)
        { 

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
            return new AddNewAreaResult(newArea.Id, newArea.Name, newArea.Code);
        }
    }
}
