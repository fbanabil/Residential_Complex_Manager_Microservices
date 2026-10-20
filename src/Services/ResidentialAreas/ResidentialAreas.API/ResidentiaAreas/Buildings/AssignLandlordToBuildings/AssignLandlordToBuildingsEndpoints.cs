
namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AssignLandlordToBuildings
{
    public record AssignLandlordToBuildingRequest(long BuildingCode, string Email);
    public record AssignLandlordToBuildingResponse(bool Success, string Message);


    public class AssignLandlordToBuildingRequestValidatior : AbstractValidator<AssignLandlordToBuildingRequest>
    {
        public AssignLandlordToBuildingRequestValidatior()
        {
            RuleFor(x => x.BuildingCode).GreaterThanOrEqualTo(2000000000).WithMessage("Building code must be greater than or equal to 2000000000.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(x => x.BuildingCode).LessThan(3000000000).WithMessage("Building code must be less than 3000000000.");
        }
    }

    public class AssignLandlordToBuildingsEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/residential-areas/building/assign-landlord", HandleAssignLandlordToBuilding)
                .WithName("AssignLandlordToBuilding")
                .WithTags("Buildings")
                .WithSummary("Assigns a landlord to a building.")
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .RequireAuthorization("AdminOrComplexManager");
        }

        private static async Task<IResult> HandleAssignLandlordToBuilding(AssignLandlordToBuildingRequest request, ISender sender, IValidator<AssignLandlordToBuildingRequest> validator, ILogger<AssignLandlordToBuildingsEndpoints> logger)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                logger.LogWarning("Assign landlord to building failed: validation error for building code {BuildingCode} and email {Email}", request.BuildingCode, request.Email);
                return Results.ValidationProblem(validationResult.ToDictionary());
            }


            var command = request.Adapt<AssignLandlordToBuildingCommand>();


            var result = await sender.Send(command);
            if (result.Error is not null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }
            var response = result.Result.Adapt<AssignLandlordToBuildingResponse>();

            return Results.Ok(response);
        }
    }
}
