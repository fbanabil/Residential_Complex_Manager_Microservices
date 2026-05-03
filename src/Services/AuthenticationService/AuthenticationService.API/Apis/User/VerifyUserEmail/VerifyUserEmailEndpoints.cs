using Microsoft.AspNetCore.Mvc;

namespace AuthenticationService.API.Apis.User.VerifyUserEmail
{
    public record VerifyUserEmailResponse(bool Success, string Message);


    public class VerifyUserEmailEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/auth/verify-email", HandleVerifyEmail)
                .WithName("VerifyUserEmail _ 01")
                .WithTags("User Email Verification")
                .WithSummary("Verifies a user's email address using a verification token.")
               .AllowAnonymous();

            app.MapPost("/auth/verify-email", HandleVerifyEmail)
               .WithName("VerifyUserEmail _ 02")
                .WithTags("User Email Verification")
                .WithSummary("Verifies a user's email address using a verification token.")
               .AllowAnonymous();
        }

        private static async Task<IResult> HandleVerifyEmail([FromQuery] Guid userId, [FromQuery] string token, ISender sender)
        {
            var IsGuidValid = Guid.TryParse(userId.ToString(), out Guid validUserId);
            if ( (!IsGuidValid) || string.IsNullOrEmpty(token))
            {
                return Results.Problem(detail: "The user ID must be a valid GUID and the token must not be empty.", statusCode: 400, title: "INVALID_REQUEST");
            }

            var command = new VerifyUserEmailCommand(validUserId, token);

            var result = await sender.Send(command);

            if (result.ErrorCarrier is not null)
            {
                return Results.Problem(detail: result.ErrorCarrier.Detail, statusCode: result.ErrorCarrier.StatusCode, title: result.ErrorCarrier.Title);
            }

            return Results.Ok(result.Result);
        }
   
    }
}
