using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea
{
    public record AddNewAreaRequest(string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, String Status, string ImageBase64);

    public record AddNewAreaResponse(Guid Id, string Name,long Code);

    public class AddNewAreaRequestValidator : AbstractValidator<AddNewAreaRequest>
    {
        private readonly ILocationValidator _locationValidator;
        public AddNewAreaRequestValidator(ILocationValidator locationValidator)
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
            RuleFor(x => x.ImageBase64).NotEmpty().WithMessage("The image is required.")
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringImage(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }

     
    }

    public class AddNewAreaEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/areas", async (AddNewAreaRequest request, ISender sender,[FromServices] IValidator<AddNewAreaRequest> validator) =>
            {
               var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<AddNewAreaCommand>();
                var result = await sender.Send(command);
                var response = result.Adapt<AddNewAreaResponse>();

                if(response == null)
                {
                    return Results.Problem("An error occurred while creating the area.", statusCode: StatusCodes.Status500InternalServerError);
                }
                
                if(response.Id == Guid.Empty)
                {
                    return Results.Problem("Failed to create the area. Please check the provided data and try again.", statusCode: StatusCodes.Status400BadRequest);
                }

                return Results.Created($"/areas/{response!.Id}", response);

            })
                .WithName("AddNewArea")
                .WithTags("Areas")
                .Produces<AddNewAreaResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Adds a new residential area to the system.")
                .WithDescription("This endpoint allows clients to add a new residential area by providing the necessary details such as name, location, and status.");
        }
    }
}
