using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AssignBuildingToArea
{
    public record AssignBuildingToAreaRequest(long AreaCode, List<long> BuildingCodes);
    public record AssignBuildingToAreaResponse(bool Success, string Message);

    public class AssignBuildingToAreaValidator : AbstractValidator<AssignBuildingToAreaRequest>
    {
        public AssignBuildingToAreaValidator()
        {
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).WithMessage("Area code must be greater than or equal to 1000000000.");
            RuleFor(x => x.AreaCode).LessThan(2000000000).WithMessage("Area code must be less than 2000000000.");

            RuleFor(x => x.BuildingCodes)
                .NotEmpty().WithMessage("At least one building code is required.");

            RuleForEach(x => x.BuildingCodes)
                .GreaterThanOrEqualTo(2000000000).WithMessage("Building code must be greater than or equal to 2000000000.")
                .LessThan(3000000000).WithMessage("Building code must be less than 3000000000.");
        }
    }

    public class AssignBuildingToAreaEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/buildings/assign-to-area", HandleAssignBuildingToArea)
                .WithName("AssignBuildingToArea")
                .WithTags("Buildings")
                .WithSummary("Assigns buildings to an area.")
                .Produces<AssignBuildingToAreaResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireAuthorization("AdminOrComplexManager");
        }

        private static async Task<IResult> HandleAssignBuildingToArea(AssignBuildingToAreaRequest request, ISender sender, [FromServices] IValidator<AssignBuildingToAreaRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var command = request.Adapt<AssignBuildingToAreaCommand>();
            var result = await sender.Send(command);

            if (result.Error != null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }

            var response = result.Result?.Adapt<AssignBuildingToAreaResponse>();
            return Results.Ok(response);
        }
    }
}