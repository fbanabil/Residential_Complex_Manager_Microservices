using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Apis.Role.AssignRole
{
    public record AssignRoleRequest(string UserEmail, string RoleName);
    public record AssignRoleResponse(bool Success, string Message);


    public class AssigRoleRequestValidator : AbstractValidator<AssignRoleRequest>
    {
        public AssigRoleRequestValidator()
        {
            RuleFor(x => x.UserEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.RoleName).NotEmpty();
        }
    }

    public class AssignRoleEndpoints : ICarterModule
    {
        private readonly ILogger<AssignRoleEndpoints> _logger;

        public AssignRoleEndpoints(ILogger<AssignRoleEndpoints> logger)
        {
            _logger = logger;
        }

        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/roles/assign", HandleRoleAssign)
                .WithName("AssignRole")
                .WithTags("Role Management")
                .WithSummary("Assign a role to a user")
                .Produces<AssignRoleResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .RequireAuthorization("AdminOnly");
        }


        private async Task<IResult> HandleRoleAssign(AssignRoleRequest request, ISender sender, [FromServices] IValidator<AssignRoleRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Assign role failed: validation error for email {Email} and role {RoleName}", request.UserEmail, request.RoleName);
                return Results.ValidationProblem(validationResult.ToDictionary());
            }
            var command = request.Adapt<AssignRoleCommand>();
            var result = await sender.Send(command);

            if (result.Error != null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }

            var response = result.Result?.Adapt<AssignRoleResponse>();
            return Results.Ok(response);
        }
    }
}
