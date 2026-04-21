using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.UpdateParkingSpaceById
{
    public record UpdateParkingSpaceByIdRequest(Guid Id, long AreaCode, string Name, string? Description, string BlockNo, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages);
    public record UpdateParkingSpaceByIdResponse(Guid Id, long ParkingSpaceCode, string Name, string? Description, string BlockNo, string Status, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class UpdateParkingSpaceByIdValidator : AbstractValidator<UpdateParkingSpaceByIdRequest>
    {
        public UpdateParkingSpaceByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Parking space Id is required.");

            RuleFor(x => x.AreaCode).NotEmpty().WithMessage("Area code is required.");
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).WithMessage("Area code must be greater than or equal to 1000000000.");
            RuleFor(x => x.AreaCode).LessThan(2000000000).WithMessage("Area code must be less than 2000000000.");

            RuleFor(x => x.Name).NotEmpty().WithMessage("The parking space name is required.");
            RuleFor(x => x.BlockNo).NotEmpty().WithMessage("The block number is required.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("The status is required.")
                .IsEnumName(typeof(Status)).WithMessage("The status must be a valid value (Active, Inactive, Maintenance).");

            RuleFor(x => x.AddedBase64StringImages)
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringLiset(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }
    }

    public class UpdateParkingSpaceByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/parking-spaces/update-by-id", async (HttpContext httpContext, UpdateParkingSpaceByIdRequest request, ISender sender, [FromServices] IValidator<UpdateParkingSpaceByIdRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<UpdateParkingSpaceByIdCommand>();
                var result = await sender.Send(command);

                if(result.ErrorCarrier != null)
                {
                    return Results.Problem(detail: result.ErrorCarrier.Detail, statusCode: result.ErrorCarrier.StatusCode, title: result.ErrorCarrier.Title);
                }

                var response = result.Result?.Adapt<UpdateParkingSpaceByIdResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("UpdateParkingSpaceById")
                .WithTags("ParkingSpaces")
                .Produces<UpdateParkingSpaceByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Updates a parking space by its ID.");
        }
    }
}