
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Apis.User.ResetPassword
{
    public record ResetPasswordRequest(string Email);
    public record ResetPasswordResponse(bool Success, string Message);

    public record NewRandomPasswordResponse(string NewPassword);

    public class EmailValidator : AbstractValidator<string>
    {
        public EmailValidator()
        {
            RuleFor(email => email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");
        }
    }

    public class ResetPasswordEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/user/reset-password", async ([FromBody] ResetPasswordRequest request, ISender sender, IValidator<string> emailValidator) =>
            {
                var validationResult = emailValidator.Validate(request.Email);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = new ResetPasswordCommand(request.Email);
                
                
                var result = await sender.Send(command);
                if (result.Error != null)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.Error.Detail);
                }

                if (result.Result == null)
                {
                    return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "An unexpected error occurred.");
                }

                return Results.Ok(result.Result);
            });


            app.MapPost("/user/reset-password/confirm", async ([FromQuery]Guid userId, [FromQuery]string token, ISender sender) =>
            {


                var command = new ResetPasswordConfirmCommand(userId, token);
                
                var result = await sender.Send(command);
                if (result.Error != null)
                {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.Error.Detail);
                }

                if (result.NewPassword == null)
                {
                    return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: "An unexpected error occurred.");
                }

                return Results.Ok(new NewRandomPasswordResponse(result.NewPassword));
            });
        }
    }
}
