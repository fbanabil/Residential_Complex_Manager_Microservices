using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.UpdateBuildingByCode
{
    public record UpdateBuildingByCodeRequest(long Code, string Name, string BlockNo, int TotalFloors, string Address, string Status, List<string?>? RemovedImagesUrls, List<string?>? AddedBase64StringImages);
    public record UpdateBuildingByCodeResponse(Guid Id, long Code, string Name, string BlockNo, int? TotalFloors, string Address, string Status, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class UpdateBuildingByCodeValidator : AbstractValidator<UpdateBuildingByCodeRequest>
    {
        public UpdateBuildingByCodeValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("The building code is required.");
            RuleFor(x => x.Code).GreaterThanOrEqualTo(2000000000).WithMessage("Wrong format of Code.")
                .LessThan(3000000000).WithMessage("Wrong format of Code.");
            RuleFor(x => x.Name).NotEmpty().WithMessage("The building name is required.")
                .MaximumLength(100).WithMessage("The building name cannot exceed 100 characters.");
            RuleFor(x => x.BlockNo).NotEmpty().WithMessage("The block number is required.")
                .MaximumLength(30).WithMessage("The block number cannot exceed 30 characters.");
            RuleFor(x => x.TotalFloors).GreaterThan(0).WithMessage("Total floors must be greater than 0.");
            RuleFor(x => x.Address).NotEmpty().WithMessage("The address is required.");
            RuleFor(x => x.Status).NotEmpty().WithMessage("The status is required.")
                .IsEnumName(typeof(Status)).WithMessage("The status must be a valid value (Active, Inactive, Maintenance).");
            RuleFor(x => x.AddedBase64StringImages)
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringList(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
            RuleFor(x => x.AddedBase64StringImages).Must(x => x == null || x.Count <= 10).WithMessage("You can add a maximum of 10 images.");
            RuleFor(x => x.RemovedImagesUrls).Must(x => x == null || x.Count <= 10).WithMessage("You can remove a maximum of 10 images.");
            RuleForEach(x => x.RemovedImagesUrls).Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _)).WithMessage("Removed image URL must be a valid URL.");
        }
    }

    public class UpdateBuildingByCodeEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/buildings/update-by-code", async (UpdateBuildingByCodeRequest request, ISender sender, [FromServices] IValidator<UpdateBuildingByCodeRequest> validator, CancellationToken cancellationToken) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<UpdateBuildingByCodeCommand>();

                var result = await sender.Send(command, cancellationToken);
                if (result.Error != null)
                {
                    return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
                }

                var response = result.Result.Adapt<UpdateBuildingByCodeResponse>();

                return Results.Ok(response);
            })
                .WithName("UpdateBuildingByCode")
                .WithTags("Buildings")
                .Produces<UpdateBuildingByCodeResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Updates a building by its code.")
                .RequireAuthorization("AdminOrComplexManagerOrTenant");
        }
    }
}