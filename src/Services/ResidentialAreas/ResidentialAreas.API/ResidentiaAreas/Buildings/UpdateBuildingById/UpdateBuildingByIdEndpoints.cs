using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.UpdateBuildingById
{
    public record UpdateBuildingByIdRequest(Guid Id, string Name, string BlockNo, int TotalFloors, string Address, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages);
    public record UpdateBuildingByIdResponse(Guid Id, long Code, string Name, string BlockNo, int? TotalFloors, string Address, string Status, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class UpdateBuildingByIdValidator : AbstractValidator<UpdateBuildingByIdRequest>
    {
        public UpdateBuildingByIdValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("The building name is required.")
                .MaximumLength(100).WithMessage("The building name cannot exceed 100 characters.");
            RuleFor(x => x.BlockNo).NotEmpty().WithMessage("The block number is required.")
                .MaximumLength(30).WithMessage("The block number cannot exceed 30 characters.");
            RuleFor(x => x.TotalFloors).GreaterThan(0).WithMessage("Total floors must be greater than 0.");
            RuleFor(x => x.Address).NotEmpty().WithMessage("The address is required.");
            RuleFor(x => x.Status).NotEmpty().WithMessage("The status is required.")
                .IsEnumName(typeof(Status)).WithMessage("The status must be a valid value (Active, Inactive, Maintenance).");
            RuleFor(x => x.AddedBase64StringImages)
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringLiset(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }
    }

    public class UpdateBuildingByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/buildings/update-by-id", async (HttpContext httpContext, UpdateBuildingByIdRequest request, ISender sender, [FromServices] IValidator<UpdateBuildingByIdRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<UpdateBuildingByIdCommand>();
                var result = await sender.Send(command);

                if (result == null)
                {
                    return Results.NotFound("The building with the specified ID was not found.");
                }

                var response = result.Adapt<UpdateBuildingByIdResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("UpdateBuildingById")
                .WithTags("Buildings")
                .Produces<UpdateBuildingByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Updates a building by its ID.")
                .RequireAuthorization("AdminOrComplexManagerOrTenant");
        }
    }
}
