using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.Helpers.Image;


namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AddNewFacility
{
    public record AddNewFacilityRequest(long? AreaCode, long? BuildingCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, List<string?>? ImageBase64);

    public record AddNewFacilityResponse(Guid Id, long FacilityCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, long? AreaCode, string? AreaName, long? BuildingCode, string? BuildingName, List<string?>? ImageUrls);

    public class AddNewFacilityValidator : AbstractValidator<AddNewFacilityRequest>
    {
        public AddNewFacilityValidator()
        {
            RuleFor(x => x)
                .Must(x => x.AreaCode.HasValue || x.BuildingCode.HasValue)
                .WithMessage("Either AreaCode or BuildingCode is required.");

            RuleFor(x => x)
                .Must(x => !(x.AreaCode.HasValue && x.BuildingCode.HasValue))
                .WithMessage("Only one parent can be specified. Provide either AreaCode or BuildingCode.");

            RuleFor(x => x.AreaCode)
                .GreaterThanOrEqualTo(1000000000)
                .When(x => x.AreaCode.HasValue)
                .WithMessage("Area code must be greater than or equal to 1000000000.");

            RuleFor(x => x.AreaCode)
                .LessThan(2000000000)
                .When(x => x.AreaCode.HasValue)
                .WithMessage("Area code must be less than 2000000000.");

            RuleFor(x => x.BuildingCode)
                .GreaterThanOrEqualTo(2000000000)
                .When(x => x.BuildingCode.HasValue)
                .WithMessage("Building code must be greater than or equal to 2000000000.");

            RuleFor(x => x.BuildingCode)
                .LessThan(3000000000)
                .When(x => x.BuildingCode.HasValue)
                .WithMessage("Building code must be less than 3000000000.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Facility name is required.")
                .MaximumLength(100).WithMessage("Facility name cannot exceed 100 characters.");

            RuleFor(x => x.FacilityType)
                .NotEmpty().WithMessage("Facility type is required.")
                .MaximumLength(30).WithMessage("Facility type cannot exceed 30 characters.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0).WithMessage("Capacity must be greater than 0.");

            RuleFor(x => x.HourlyRate)
                .GreaterThanOrEqualTo(0)
                .When(x => x.HourlyRate.HasValue)
                .WithMessage("Hourly rate must be greater than or equal to 0.");

            RuleFor(x => x.Rules)
                .Must(BeValidJson)
                .When(x => !string.IsNullOrWhiteSpace(x.Rules))
                .WithMessage("Rules must be a valid JSON string.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .IsEnumName(typeof(Status)).WithMessage("Status must be a valid value (Active, Inactive, Maintenance).");

            RuleFor(x => x.ImageBase64)
                .NotEmpty().WithMessage("At least one image is required.")
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringList(imageBase64)))
                .WithMessage("The image must be a valid Base64 string.");
        }

        private static bool BeValidJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return true;
            }

            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class AddNewFacilityEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/residential-areas/facilities/add", async (AddNewFacilityRequest request, ISender sender, [FromServices] IValidator<AddNewFacilityRequest> validator, CancellationToken cancellationToken, ILogger<AddNewFacilityEndpoints> logger) =>
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    logger.LogWarning("Add new facility failed: validation error for facility '{FacilityName}'", request.Name);
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<AddNewFacilityCommand>();
                var result = await sender.Send(command, cancellationToken);

                if (result.Error != null)
                {
                    return Results.Problem(title: result.Error.Title, detail: result.Error.Detail, statusCode: result.Error.StatusCode);
                }

                var response = result.Result.Adapt<AddNewFacilityResponse>();

                return Results.Created($"/facilities/{response!.Id}", response);
            })
                .WithName("AddNewFacility")
                .WithTags("Facilities")
                .Produces<AddNewFacilityResponse>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Adds a new facility under an area or a building.")
                .RequireAuthorization("AdminOrComplexManagerOrTenant");
        }
    }
}
