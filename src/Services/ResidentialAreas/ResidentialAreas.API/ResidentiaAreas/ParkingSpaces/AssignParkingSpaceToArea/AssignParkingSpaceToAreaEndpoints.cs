using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.AssignParkingSpaceToArea
{
    public record AssignParkingSpaceToAreaRequest(long AreaCode, long ParkingSpaceCode);
    public record AssignParkingSpaceToAreaResponse(bool Success, string Message);

    public class AssignParkingSpaceToAreaValidator : AbstractValidator<AssignParkingSpaceToAreaRequest>
    {
        public AssignParkingSpaceToAreaValidator()
        {
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).WithMessage("Area code must be greater than or equal to 1000000000.");
            RuleFor(x => x.AreaCode).LessThan(2000000000).WithMessage("Area code must be less than 2000000000.");

            RuleFor(x => x.ParkingSpaceCode).GreaterThanOrEqualTo(3000000000).WithMessage("Parking space code must be greater than or equal to 3000000000.");
            RuleFor(x => x.ParkingSpaceCode).LessThan(4000000000).WithMessage("Parking space code must be less than 4000000000.");
        }
    }

    public class AssignParkingSpaceToAreaEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/parking-spaces/assign-to-area", HandleAssignParkingSpaceToArea)
                .WithName("AssignParkingSpaceToArea")
                .WithTags("ParkingSpaces")
                .WithSummary("Assigns a parking space to an area.")
                .Produces<AssignParkingSpaceToAreaResponse>(StatusCodes.Status200OK)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireAuthorization("AdminOrComplexManager");
        }

        private static async Task<IResult> HandleAssignParkingSpaceToArea(AssignParkingSpaceToAreaRequest request, ISender sender, [FromServices] IValidator<AssignParkingSpaceToAreaRequest> validator, CancellationToken cancellationToken)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var command = request.Adapt<AssignParkingSpaceToAreaCommand>();
            var result = await sender.Send(command, cancellationToken);

            if (result.Error != null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }

            var response = result.Result?.Adapt<AssignParkingSpaceToAreaResponse>();
            return Results.Ok(response);
        }
    }
}