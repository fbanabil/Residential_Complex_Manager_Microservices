using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.AddNewParkingSpace
{
    public record AddNewParkingSpaceRequest(long AreaCode, string Name, string? Description, string BlockNo, string Status, List<string?>? ImageBase64);
    public record AddNewParkingSpaceResponse(Guid Id, long ParkingSpaceCode, string Name, string? Description, string BlockNo, string Status, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class AddNewParkingSpaceValidator : AbstractValidator<AddNewParkingSpaceRequest>
    {
        public AddNewParkingSpaceValidator()
        {
            RuleFor(x => x.AreaCode).NotEmpty().WithMessage("Area code is required.");
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).WithMessage("Area code must be greater than or equal to 1000000000.");
            RuleFor(x => x.AreaCode).LessThan(2000000000).WithMessage("Area code must be less than 2000000000.");

            RuleFor(x => x.Name).NotEmpty().WithMessage("Parking space name is required.");
            RuleFor(x => x.BlockNo).NotEmpty().WithMessage("Block number is required.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .IsEnumName(typeof(Status)).WithMessage("Status must be a valid value (Active, Inactive, Maintenance).");

            RuleFor(x => x.ImageBase64)
                .NotEmpty().WithMessage("At least one image is required.")
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringLiset(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }
    }

    public class AddNewParkingSpaceEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/parking-spaces/add", async (HttpContext httpContext, AddNewParkingSpaceRequest request, ISender sender, [FromServices] IValidator<AddNewParkingSpaceRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<AddNewParkingSpaceCommand>();
                var result = await sender.Send(command);


                if(result.ErrorCarrier != null)
                {
                    return Results.Problem(detail: result.ErrorCarrier.Detail, statusCode: result.ErrorCarrier.StatusCode, title: result.ErrorCarrier.Title);
                }


                var response = result.Result.Adapt<AddNewParkingSpaceResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Created($"/parking-spaces/{response.Id}", response);
            })
                .WithName("AddNewParkingSpace")
                .WithTags("ParkingSpaces")
                .Produces<AddNewParkingSpaceResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Adds a new parking space under an area.");
        }
    }
}
