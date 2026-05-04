using AuthenticationService.API.Helpers.GetHostUrl;
using Mapster;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace AuthenticationService.API.Apis.User.OAuthLogins
{

    public record OAuthLoginResponse(string AccessToken, string? RefreshToken, string Message);

    public class OAuthLoginsEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/auth/login-google", async (HttpContext httpContext, IGetHostUrl getHostUrl) =>
            {
                var redirectUrl = await getHostUrl.GetHostUrlAsync() + "/auth/signin-google";
                var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
                return Results.Challenge(properties, new[] { GoogleDefaults.AuthenticationScheme });
            })
                .WithName("OAuth Login")
                .WithTags("OAuth")
                .AllowAnonymous();




            app.MapGet("/auth/signin-google", async (HttpContext httpContext, ISender sender, IGetHostUrl getHostUrl) =>
            {
                // Authenticate the user with Google
                var result = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                if (!result.Succeeded || result?.Principal == null)
                {
                    return Results.Problem(title: "GOOGLE_AUTHENTICATION_FAILED", statusCode: StatusCodes.Status400BadRequest, detail: "Unable to authenticate with Google.");
                }


                // Extract the email and Google ID claims from the authentication result
                var emailClaim = result.Principal.FindFirst(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Results.Problem(title: "EMAIL_CLAIM_NOT_FOUND", statusCode: StatusCodes.Status400BadRequest, detail: "Email claim not found in Google authentication result.");
                }
                var email = emailClaim.Value;


                // Extract the Google ID claim from the authentication result
                var googleIdClaim = result.Principal.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);
                if (googleIdClaim == null)
                {
                    return Results.Problem(title: "GOOGLE_ID_CLAIM_NOT_FOUND", statusCode: StatusCodes.Status400BadRequest, detail: "Google ID claim not found in Google authentication result.");
                }
                var google = googleIdClaim.Value;


                // Create a command to handle the OAuth login logic
                var command = new OAuthLoginsCommand(GoogleId : google, Email: email, Name: result.Principal.FindFirst(c => c.Type == ClaimTypes.Name)?.Value, PictureUrl: result.Principal.FindFirst(c => c.Type == "picture")?.Value);


                // Send the command to the handler and get the result
                var resultFromHandler = await sender.Send(command);
                if (resultFromHandler.Error != null)
                {
                    return Results.Problem(title: resultFromHandler.Error.Title, statusCode: resultFromHandler.Error.StatusCode, detail: resultFromHandler.Error.Detail);
                }

                // Map the login response from the handler to the OAuthLoginResponse model
                var response = resultFromHandler.LoginResponse.Adapt<OAuthLoginResponse>();


                // Set the access token in the response header and the refresh token in an HTTP-only cookie
                var cookiesOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                // Set the refresh token in an HTTP-only cookie if it exists and the access token in the response header
                if (response!.RefreshToken != null)
                {
                    httpContext.Response.Cookies.Append("refreshToken", response.RefreshToken!, cookiesOptions);
                }
                httpContext.Response.Headers.Append("Authorization", $"Bearer {response.AccessToken}");


                // Return the access token in the response body
                return Results.Ok(response.AccessToken);
            })
                .WithName("OAuth Sining")
                .WithTags("OAuth")
                .AllowAnonymous(); ;
        }
    }
}
