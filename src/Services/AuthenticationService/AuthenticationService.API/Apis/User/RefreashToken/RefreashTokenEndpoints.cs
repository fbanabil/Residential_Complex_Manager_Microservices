using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Apis.User.RefreashToken
{
    public record RefreashTokenResponse(string AccessToken);

    public class EmailValidator : AbstractValidator<string>
    {
        public EmailValidator()
        {
            RuleFor(email => email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }

    public class RefreashTokenEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/user/refreash-token", async (HttpContext httpContext, [FromQuery] string email, ISender sender, IValidator<string> emailValidator) =>
            {
                var validationResult = emailValidator.Validate(email);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }


                string? refreashToken = httpContext?.Request?.Cookies["refreshToken"]?.ToString() ?? null;
                if (string.IsNullOrEmpty(refreashToken))
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Refreash token is required");
                }


                var command = new RefreashTokenCommand(email, refreashToken);
                var result = await sender.Send(command);

                
                if (result.Error != null)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.Error.Detail);
                }


                if(result.Result == null)
                {
                    return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "An unexpected error occurred.");
                }


                httpContext?.Response.Headers.Append("Authorization", $"Bearer {result.Result.AccessToken}");
                
                return Results.Ok(result.Result!.AccessToken);
            })
                .WithName("RefreashToken")
                .WithTags("User")
                .AllowAnonymous();
        }
    }
}
