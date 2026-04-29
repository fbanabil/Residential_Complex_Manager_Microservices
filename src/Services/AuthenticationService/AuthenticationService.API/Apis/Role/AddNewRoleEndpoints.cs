
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Apis.Role
{
    public record AddNewRoleRequest(string Name, string Description);
    public record AddNewRoleResponse(Guid Id, string Name, string Description);


    public class AddNewRoleValidator : AbstractValidator<AddNewRoleRequest>
    {
        public AddNewRoleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("The role name is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("The role description is required.");
        }
    }   

    public class AddNewRoleEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/roles/add-new", async (AddNewRoleRequest request, ISender sender, [FromServices] IValidator<AddNewRoleRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }
                var command = request.Adapt<AddNewRoleCommand>();
                var result = await sender.Send(command);
                if (result.ErrorCarrier != null)
                {
                    return Results.Problem(detail: result.ErrorCarrier.Detail, statusCode: result.ErrorCarrier.StatusCode, title: result.ErrorCarrier.Title);
                }
                var response = result.Result?.Adapt<AddNewRoleResponse>();
                return Results.Ok(response);
            })
                .WithName("AddNewRole")
                .WithTags("Role Management")
                .Produces<AddNewRoleResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status500InternalServerError)
                .WithSummary("Adds a new role to the system.")
                .WithDescription("This endpoint allows you to add a new role by providing its name and description.")
                .AllowAnonymous();


        }
    }
}
