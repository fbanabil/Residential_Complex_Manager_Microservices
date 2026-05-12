
namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AssignTenantToBuildings
{
    public record AssignTenantToBuildingRequest(long BuildingCode, string Email);
    public record AssignTenantToBuildingResponse(bool Success, string Message);


    public class AssignTenantToBuildingRequestValidatior : AbstractValidator<AssignTenantToBuildingRequest>
    {
        public AssignTenantToBuildingRequestValidatior()
        {
            RuleFor(x => x.BuildingCode).GreaterThanOrEqualTo(2000000000).WithMessage("Building code must be greater than or equal to 2000000000.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(x => x.BuildingCode).LessThan(3000000000).WithMessage("Building code must be less than 3000000000.");
        }
    }

    public class AssignTenantToBuildingsEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/building/assign-tenant", HandleAssignTenantToBuilding)
                .WithName("AssignTenantToBuilding")
                .WithTags("Buildings")
                .WithSummary("Assigns a tenant to a building.")
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .RequireAuthorization("AdminOrComplexManager");
        }

        private static async Task<IResult> HandleAssignTenantToBuilding(AssignTenantToBuildingRequest request, ISender sender, IValidator<AssignTenantToBuildingRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }


            var command = request.Adapt<AssignTenantToBuildingCommand>();
            
            
            var result = await sender.Send(command);
            if (result.Error is not null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }
            var response = result.Result.Adapt<AssignTenantToBuildingResponse>();
            
            
            return Results.Ok(response);
        }
    }
}
