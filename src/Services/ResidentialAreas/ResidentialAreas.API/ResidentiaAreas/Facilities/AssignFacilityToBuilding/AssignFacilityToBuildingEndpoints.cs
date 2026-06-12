using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AssignFacilityToBuilding
{
    public record AssignFacilityToBuildingRequest(long FacilityCode, long BuildingCode);
    public record AssignFacilityToBuildingResponse(bool Success, string Message);

    public class AssignFacilityToBuildingValidator : AbstractValidator<AssignFacilityToBuildingRequest>
    {
        public AssignFacilityToBuildingValidator()
        {
            RuleFor(x => x.FacilityCode)
                .GreaterThan(0).WithMessage("Facility code must be a positive number.");

            RuleFor(x => x.BuildingCode).GreaterThanOrEqualTo(2000000000).WithMessage("Building code must be greater than or equal to 2000000000.");
            RuleFor(x => x.BuildingCode).LessThan(3000000000).WithMessage("Building code must be less than 3000000000.");
        }
    }

    public class AssignFacilityToBuildingEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/residential-areas/facilities/assign-to-building", HandleAssignFacilityToBuilding)
                .WithName("AssignFacilityToBuilding")
                .WithTags("Facilities")
                .WithSummary("Assigns a facility to a building.")
                .Produces<AssignFacilityToBuildingResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireAuthorization("AdminOrComplexManager");
        }

        private static async Task<IResult> HandleAssignFacilityToBuilding(AssignFacilityToBuildingRequest request, ISender sender, [FromServices] IValidator<AssignFacilityToBuildingRequest> validator, CancellationToken cancellationToken, ILogger<AssignFacilityToBuildingEndpoints> logger)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Assign facility to building failed: validation error for facility code {FacilityCode} and building code {BuildingCode}", request.FacilityCode, request.BuildingCode);
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var command = request.Adapt<AssignFacilityToBuildingCommand>();
            var result = await sender.Send(command, cancellationToken);

            if (result.Error != null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }

            var response = result.Result?.Adapt<AssignFacilityToBuildingResponse>();
            return Results.Ok(response);
        }
    }
}
