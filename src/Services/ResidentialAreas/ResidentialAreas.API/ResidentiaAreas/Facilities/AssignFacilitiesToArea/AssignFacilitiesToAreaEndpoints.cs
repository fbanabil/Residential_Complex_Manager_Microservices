using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AssignFacilitiesToArea
{
    public record AssignFacilitiesToAreaRequest(long AreaCode, List<long> FacilityCodes);
    public record AssignFacilitiesToAreaResponse(bool Success, string Message);

    public class AssignFacilitiesToAreaValidator : AbstractValidator<AssignFacilitiesToAreaRequest>
    {
        public AssignFacilitiesToAreaValidator()
        {
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).WithMessage("Area code must be greater than or equal to 1000000000.");
            RuleFor(x => x.AreaCode).LessThan(2000000000).WithMessage("Area code must be less than 2000000000.");

            RuleFor(x => x.FacilityCodes)
                .NotEmpty().WithMessage("At least one facility code is required.");

            RuleForEach(x => x.FacilityCodes)
                .GreaterThan(0).WithMessage("Facility code must be a positive number.");
        }
    }

    public class AssignFacilitiesToAreaEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/residential-areas/facilities/assign-to-area", HandleAssignFacilitiesToArea)
                .WithName("AssignFacilitiesToArea")
                .WithTags("Facilities")
                .WithSummary("Assigns facilities to an area.")
                .Produces<AssignFacilitiesToAreaResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireAuthorization("AdminOrComplexManager");
        }

        private static async Task<IResult> HandleAssignFacilitiesToArea(AssignFacilitiesToAreaRequest request, ISender sender, [FromServices] IValidator<AssignFacilitiesToAreaRequest> validator, ILogger<AssignFacilitiesToAreaEndpoints> logger)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Assign facilities to area failed: validation error for area code {AreaCode}", request.AreaCode);
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var command = request.Adapt<AssignFacilitiesToAreaCommand>();
            var result = await sender.Send(command);

            if (result.Error != null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }

            var response = result.Result?.Adapt<AssignFacilitiesToAreaResponse>();
            return Results.Ok(response);
        }
    }
}
