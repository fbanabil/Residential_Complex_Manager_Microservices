using AuthenticationService.API.Enum;
using AuthenticationService.API.Helpers.ErrorCarrier;
using AuthenticationService.API.Helpers.GetHostUrl;
using AuthenticationService.API.Helpers.Image;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using MediatR;
using Carter;

namespace AuthenticationService.API.Apis.User.AddNewUser
{

    public record RegisterUserRequest(
        string UserName,
        string Email,
        string Password,
        string ConfirmPassword,
        string Phone,
        string Bas64ProfileImage,
        string Base64NidImage
    );

    public record RegisterUserResponse(
        Guid UserId,
        string UserName,
        string Email,
        string Message
    );

    public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
    {
        public RegisterUserRequestValidator()
        {
            RuleFor(x => x.UserName).NotEmpty().WithMessage("The user name is required.")
                .MaximumLength(100).WithMessage("The user name cannot exceed 100 characters.");
            RuleFor(x => x.Email).NotEmpty().WithMessage("The email is required.")
                .EmailAddress().WithMessage("The email must be a valid email address.");
            RuleFor(x => x.Password).NotEmpty().WithMessage("The password is required.")
                .MinimumLength(8).WithMessage("The password must be at least 8 characters long.");
            RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage("The confirm password is required.")
                .Equal(x => x.Password).WithMessage("The confirm password must match the password.");
            RuleFor(x=>x.Password ).Must(password => password.Any(char.IsUpper)).WithMessage("The password must contain at least one uppercase letter.")
                .Must(password => password.Any(char.IsLower)).WithMessage("The password must contain at least one lowercase letter.")
                .Must(password => password.Any(char.IsDigit)).WithMessage("The password must contain at least one digit.")
                .Must(password => password.Any(ch => !char.IsLetterOrDigit(ch))).WithMessage("The password must contain at least one special character.");
            RuleFor(x => x.Phone).NotEmpty().WithMessage("The phone number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("The phone number must be a valid international phone number.");
            RuleFor(x => x.Bas64ProfileImage).NotEmpty().WithMessage("The profile image is required.")
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringImage(imageBase64)))
                .WithMessage("The profile image must be a valid Base64 string.");
            RuleFor(x => x.Base64NidImage).NotEmpty().WithMessage("The NID image is required.")
                .MustAsync(async (imageBase64, cancellation) => await Task.FromResult(Base64StringImageValidator.IsBase64StringImage(imageBase64)))
                .WithMessage("The NID image must be a valid Base64 string.");
        }
    }


    public class AddNewUserEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/users/register", async (RegisterUserRequest request, ISender sender, IValidator<RegisterUserRequest> validator) =>
            {
                // Validate the request
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }


                // Map the request to the command
                var command = request.Adapt<RegisterUserCommand>();


                // Send the command to the handler
                var result = await sender.Send(command);
                if (result.ErrorCarrier != null)
                {
                    return Results.Problem(detail: result.ErrorCarrier.Detail, statusCode: result.ErrorCarrier.StatusCode, title: result.ErrorCarrier.Title);
                }

                var response = result.Response.Adapt<RegisterUserResponse>();

                return Results.Ok(response);
            })
            .WithName("RegisterUser")
            .WithTags("Authentication")
            .WithSummary("Register a new user")
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AllowAnonymous();

        }
    }
}
