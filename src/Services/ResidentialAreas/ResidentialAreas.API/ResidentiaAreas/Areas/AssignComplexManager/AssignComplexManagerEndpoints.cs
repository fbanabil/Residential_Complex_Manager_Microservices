
namespace ResidentialAreas.API.ResidentiaAreas.Areas.AssignComplexManager
{
    public record AssignComplexManagerRequest(long AreaCode, string Email);
    public record AssignComplexManagerResponse(bool Success, string Message);


    public class AssignComplexManagerValidator : AbstractValidator<AssignComplexManagerRequest>
    {
        public AssignComplexManagerValidator()
        {
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).WithMessage("Area code must be greater than 1000000000.");
            RuleFor(x=>x.AreaCode).LessThan(2000000000).WithMessage("Area code must be less than 2000000000.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        }
    }
    public class AssignComplexManagerEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/residential-areas/areas/assign-complex-manager", HandleAssignComplexManager)
                .WithName("AssignComplexManager")
                .WithTags("Areas")
                .WithSummary("Assigns a complex manager to an area.")
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .RequireAuthorization("AdminOnly");
        }

        private static async Task<IResult> HandleAssignComplexManager(AssignComplexManagerRequest request, ISender sender, IValidator<AssignComplexManagerRequest> validator, ILogger<AssignComplexManagerEndpoints> logger)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Assign complex manager failed: validation error for area code {AreaCode} and email {Email}", request.AreaCode, request.Email);
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var command = request.Adapt<AssignComplexManagerCommand>();

            var result = await sender.Send(command);
            if (result.Error is not null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }
            var response = result.Result.Adapt<AssignComplexManagerResponse>();

            return Results.Ok(response);
        }
    }
}
