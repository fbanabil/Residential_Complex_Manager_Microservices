using Mapster;
using MimeKit.Encodings;
using System.Security.Claims;

namespace AuthenticationService.API.Apis.User.ChangePassword
{
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);

    public record ChangePasswordResponse(bool Success, string Message);


    public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordValidator() 
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithErrorCode("NULL_CURRENT_PASSWORD");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("New password can't be empty");

            RuleFor(x => x)
                .Must(x => x.NewPassword == x.ConfirmNewPassword)
                .WithMessage("New password and confirm password must match");

            RuleFor(x => x.NewPassword)
                .Must(password => password.Any(char.IsUpper))
                .WithMessage("The password must contain at least one uppercase letter.")
                .Must(password => password.Any(char.IsLower))
                .WithMessage("The password must contain at least one lowercase letter.")
                .Must(password => password.Any(char.IsDigit))
                .WithMessage("The password must contain at least one digit.")
                .Must(password => password.Any(ch => !char.IsLetterOrDigit(ch)))
                .WithMessage("The password must contain at least one special character.");
        }
    }

    public class ChangePasswordEndPoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("auth/change-password", async (HttpContext httpContext, ChangePasswordRequest request, ISender sender, IValidator<ChangePasswordRequest> validator) =>
            {
                // Validate the request
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }



                // Get the user's email from the claims
                var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? httpContext.User.FindFirst("emailaddress")?.Value ;
                if (userEmail == null)
                {
                    return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Unauthorized: User email claim is missing");
                }




                // Map the request to the command
                var command = request.Adapt<ChangePasswordCommand>();
                command = command with { UserEmail = userEmail! };



                // Send the command to the handler
                var result = await sender.Send(command);
                if (result.Error is not null)
                {
                    return Results.Problem(statusCode: result.Error.StatusCode, detail: result.Error.Detail);
                }



                return Results.Ok(new ChangePasswordResponse(true, "Password changed successfully"));
            })
                .WithName("ChangePassword")
                .WithTags("User")
                .WithSummary("Change the password of the currently authenticated user.")
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .RequireAuthorization();
        }
    }
}
