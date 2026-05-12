
using Mapster;

namespace AuthenticationService.API.Apis.User.VerifyUser
{
    public record VerifyUserRequest(string Email);
    public record VerifyUserResponse(bool Success, string Message);

    public class VerifyUserEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/verify-user", HandleVerifyUser)
                .WithName("VerifyUser")
                .WithTags("Authentication")
                .WithSummary("Sends a verification email to the user.")
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .RequireAuthorization("AdminOrComplexManagerOrTenant");
        }

        public class VerifyUserRequestValidator : AbstractValidator<VerifyUserRequest>
        {
            public VerifyUserRequestValidator()
            {
                RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Invalid email format.");
            }
        }


        private static async Task<IResult> HandleVerifyUser(VerifyUserRequest request, ISender sender, IValidator<VerifyUserRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }
            var command = request.Adapt<VerifyUserCommand>();
            var result = await sender.Send(command);

            if (result.Error is not null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }
            var response = result.Result.Adapt<VerifyUserResponse>();
            return Results.Ok(response);
        }
    }
}
