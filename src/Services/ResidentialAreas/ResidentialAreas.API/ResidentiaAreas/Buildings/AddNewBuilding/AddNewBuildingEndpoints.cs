using ResidentialAreas.API.EntityModels;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AddNewBuilding
{
    public record AddNewBuildingCommand(long AreaCode, string Name, string BlockNo, int TotalFloors, string Address, string Status, List<string?>? ImageBase64) : ICommand<AddNewBuildingResult>;

    public record AddNewBuildingResult(Guid Id, long Code, string Name, string AreaName,long AreaCode);

    public class AddNewBuildingValidator : AbstractValidator<AddNewBuildingCommand>
    {
        public AddNewBuildingValidator()
        {
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).WithMessage("Area code must be greater than 1000000000.");
            RuleFor(x => x.AreaCode).NotEmpty().WithMessage("Area code is required.");
            RuleFor(x=>x.AreaCode).LessThan(2000000000).WithMessage("Area code must be less than 2000000000.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.BlockNo).NotEmpty().WithMessage("Block number is required.");
            RuleFor(x => x.TotalFloors).GreaterThan(0).WithMessage("Total floors must be greater than 0.");
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.");
            RuleFor(x => x.Status).NotEmpty().WithMessage("Status is required.")
                .Must(status => System.Enum.TryParse(typeof(Status), status, true, out _))
                .WithMessage("Status must be a valid enum value (e.g., Active, Inactive).");
            RuleFor(x => x.ImageBase64).NotEmpty().WithMessage("The image is required.")
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringLiset(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }
    }

    public class AddNewBuildingEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/buildings/add", async (AddNewBuildingCommand command, IMediator mediator, IValidator<AddNewBuildingCommand> validator) =>
            {
                var validationResult = await validator.ValidateAsync(command);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var result = await mediator.Send(command);

                if(result is null)
                {
                    return Results.Problem("Failed to add new building. The specified area code does not exist.");
                }

                return Results.Created($"/api/areas/{command.AreaCode}/buildings/{result.Code}", result);
            })
                .WithName("AddNewBuilding")
                .WithTags("Buildings")
                .Produces<AddNewBuildingResult>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Adds a new building to the specified area.");
        }
    }
}
