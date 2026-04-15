
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.UpdateAreaById
{
    public record UpdateAreaByIdRequest(Guid Id, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages);
    public record UpdateAreaByIdResponse(Guid Id, long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, List<string?>? ImageUrls);


    public class UpdateAreaByIdValidator : AbstractValidator<UpdateAreaByIdRequest>
    {
        private readonly ILocationValidator _locationValidator;
        public UpdateAreaByIdValidator(ILocationValidator locationValidator)
        {
            _locationValidator = locationValidator;
            RuleFor(x => x.Name).NotEmpty().WithMessage("The area name is required.")
                .MaximumLength(150).WithMessage("The area name cannot exceed 150 characters.");
            RuleFor(x => x.City).NotEmpty().WithMessage("The city is required.");
            RuleFor(x => x.State).NotEmpty().WithMessage("The state is required.");
            RuleFor(x => x.Country).NotEmpty().WithMessage("The country is required.");
            RuleFor(x => x.PostalCode).NotEmpty().WithMessage("The postal code is required.")
                .MaximumLength(20).WithMessage("The postal code cannot exceed 20 characters.");
            RuleFor(x => x.Address).NotEmpty().WithMessage("The address is required.");
            RuleFor(x => x.GeoBoundary).NotEmpty().WithMessage("The geographical boundary is required.");
            RuleFor(x => x.Status).NotEmpty().WithMessage("The status is required.");
            RuleFor(x => x.Status).IsEnumName(typeof(Status)).WithMessage("The status must be a valid value (Active, Inactive, Maintenance).");
            RuleFor(x => new { x.City, x.State, x.Country, x.PostalCode }).MustAsync(async (location, cancellation) =>
            {
                return await _locationValidator.IsValidLocationAsync(location.Country, location.State, location.City, location.PostalCode);
            }).WithMessage("The provided city, state, country, and postal code combination is not valid.");
            RuleFor(x => x.AddedBase64StringImages)
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringLiset(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }
    }



    public class UpdateAreaByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/areas/update-by-id", async (HttpContext httpContext, UpdateAreaByIdRequest request, ISender sender, [FromServices] IValidator<UpdateAreaByIdRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<UpdateAreaByIdCommand>();
                var result = await sender.Send(command);

                var response = result.Adapt<UpdateAreaByIdResponse>();

                if(response == null)
                {
                    return Results.NotFound("The area with the specified ID was not found.");
                }

                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("UpdateAreaById")
                .WithTags("Areas")
                .Produces<UpdateAreaByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Updates a residential area by its ID.");
        }
    }
}
