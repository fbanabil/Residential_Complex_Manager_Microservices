
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Apis.User.GetVerificationLink
{

    public record ResendVerificationLinkResponse(bool Success, string Message);


    public class ResendVerificationLinkValidator : AbstractValidator<string>
    {
        public ResendVerificationLinkValidator()
        {
            RuleFor(x => x).NotEmpty().WithMessage("The email must not be empty.")
                .EmailAddress().WithMessage("The email must be a valid email address.");
        }
    }  


    public class ResendVerificationLinkEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/resend-verify-email-link", HandleResendVerifyEmailLink)
                .WithName("ResendVerifyEmailLink_01")
                .WithTags("User Email Verification")
                .WithSummary("Resends a verification link for a user's email address.")
                .AllowAnonymous();

            app.MapGet("/auth/get-verify-email-link", HandleResendVerifyEmailLink)
                .WithName("ResendVerifyEmailLink_02")
                .WithTags("User Email Verification")
                .WithSummary("Resends a verification link for a user's email address.")
                .AllowAnonymous();
        }

        private static async Task<IResult> HandleResendVerifyEmailLink([FromQuery] string email, ISender sender)
        {

            var validator = new ResendVerificationLinkValidator();
            var validationResult = await validator.ValidateAsync(email);
            if (!validationResult.IsValid)
            {
                return Results.Problem(detail: validationResult.Errors.First().ErrorMessage, statusCode: 400, title: "INVALID_REQUEST");
            }

            var command = new ResendVerificationLinkCommand(email);
            var result = await sender.Send(command);

            if (result.Error is not null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }

            return Results.Ok(result.Result);
        }
    }
}
