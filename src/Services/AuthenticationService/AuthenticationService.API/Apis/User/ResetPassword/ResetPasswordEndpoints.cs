
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
            app.MapPatch("/user/reset-password", HandleResetPassword)
                .WithName("Reset Password")
                .WithTags("User Management")
                .WithSummary("Resets the password for a user.")
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .AllowAnonymous();


            app.MapPost("/user/reset-password/confirm", HandleResetPasswordConfirmation)
                .WithName("Reset Password Confirmation 01")
                .WithTags("User Management")
                .WithSummary("Confirms the password reset for a user.")
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .AllowAnonymous();


            app.MapGet("/user/reset-password/confirm", HandleResetPasswordConfirmation)
                .WithName("Reset Password Confirmation 02")
                .WithTags("User Management")
                .WithSummary("Confirms the password reset for a user.")
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .AllowAnonymous();
        }



        private async static Task<IResult> HandleResetPassword([FromBody] ResetPasswordRequest request, ISender sender, IValidator<string> emailValidator)
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
        }




        private async static Task<IResult> HandleResetPasswordConfirmation([FromQuery] Guid userId, [FromQuery] string token, ISender sender)
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
        }
    }
}
