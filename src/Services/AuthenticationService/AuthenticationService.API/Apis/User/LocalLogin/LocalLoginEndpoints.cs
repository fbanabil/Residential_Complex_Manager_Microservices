
using AuthenticationService.API.EntityModels;
using Mapster;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;

namespace AuthenticationService.API.Apis.User.LocalLogin
{

    public record LocalLoginRequest(string Email, string Password);
    public record LocalLoginResponse(string AccessToken, string? RefreshToken);


    public class LocalLoginRequestValidator : AbstractValidator<LocalLoginRequest>
    {
        public LocalLoginRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Invalid email address.").WithErrorCode("INVALID_EMAIL");
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters long.").WithErrorCode("INVALID_PASSWORD");
        }
    }


    public class LocalLoginEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/local-login", HandleLocalLogin)
                .WithName("LocalLogin")
                .WithTags("User Authentication")
                .WithSummary("Authenticates a user using their email and password.")
                .AllowAnonymous();
        }


        private static async Task<IResult> HandleLocalLogin(LocalLoginRequest request, ISender sender, HttpContext httpContext, IValidator<LocalLoginRequest> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var command = request.Adapt<LocalLoginCommand>();
            var result = await sender.Send(command);

            if (result.Error is not null)
            {
                return Results.Problem(detail: result.Error.Detail, statusCode: result.Error.StatusCode, title: result.Error.Title);
            }

            var response = result.Result.Adapt<LocalLoginResponse>();


            var CookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            if (response!.RefreshToken is not null)
            {
                httpContext.Response.Cookies.Append("refreshToken", response.RefreshToken, CookieOptions);
            }

            httpContext.Response.Headers.Append("Authorization", $"Bearer {response.AccessToken}");

            return Results.Ok(response!.AccessToken);
        }
    }
}
