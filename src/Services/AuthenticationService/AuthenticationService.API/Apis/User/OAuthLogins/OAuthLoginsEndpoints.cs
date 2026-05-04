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
            });




            app.MapGet("/auth/signin-google", async (HttpContext httpContext, ISender sender, IGetHostUrl getHostUrl) =>
            {
                var result = await httpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

                if (!result.Succeeded || result?.Principal == null)
                {
                    return Results.Problem(title: "GOOGLE_AUTHENTICATION_FAILED", statusCode: StatusCodes.Status400BadRequest, detail: "Unable to authenticate with Google.");
                }
                

                var emailClaim = result.Principal.FindFirst(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Results.Problem(title: "EMAIL_CLAIM_NOT_FOUND", statusCode: StatusCodes.Status400BadRequest, detail: "Email claim not found in Google authentication result.");
                }

                var email = emailClaim.Value;

                var googleIdClaim = result.Principal.FindFirst(c => c.Type == ClaimTypes.NameIdentifier);
                if (googleIdClaim == null)
                {
                    return Results.Problem(title: "GOOGLE_ID_CLAIM_NOT_FOUND", statusCode: StatusCodes.Status400BadRequest, detail: "Google ID claim not found in Google authentication result.");
                }
                var google = googleIdClaim.Value;


                var command = new OAuthLoginsCommand(GoogleId : google, Email: email, Name: result.Principal.FindFirst(c => c.Type == ClaimTypes.Name)?.Value, PictureUrl: result.Principal.FindFirst(c => c.Type == "picture")?.Value);



                var resultFromHandler = await sender.Send(command);

                var response = resultFromHandler.LoginResponse.Adapt<OAuthLoginResponse>();

                if (resultFromHandler.Error != null)
                {
                    return Results.Problem(title: resultFromHandler.Error.Title, statusCode: resultFromHandler.Error.StatusCode, detail: resultFromHandler.Error.Detail);
                }

                var cookiesOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };


                if(response!.RefreshToken != null)
                {
                    httpContext.Response.Cookies.Append("refreshToken", response.RefreshToken!, cookiesOptions);
                }

                httpContext.Response.Headers.Append("Authorization", $"Bearer {response.AccessToken}");

                return Results.Ok(response.AccessToken);
            });
        }
    }
}
