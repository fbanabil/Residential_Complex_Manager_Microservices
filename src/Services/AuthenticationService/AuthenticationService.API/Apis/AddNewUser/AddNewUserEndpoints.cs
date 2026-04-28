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

namespace AuthenticationService.API.Apis.AddNewUser
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
        string UserId,
        string UserName,
        string Email,
        string Phone,
        string Status,
        string ProfileImageUrl,
        string NidImageUrl
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
                .MinimumLength(6).WithMessage("The password must be at least 6 characters long.");
            RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage("The confirm password is required.")
                .Equal(x => x.Password).WithMessage("The confirm password must match the password.");
            RuleFor(x => x.Phone).NotEmpty().WithMessage("The phone number is required.")
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("The phone number must be a valid international phone number.");
            RuleFor(x => x.Status).NotEmpty().WithMessage("The status is required.")
                .IsEnumName(typeof(Status)).WithMessage("The status must be a valid value (Active, Inactive, Banned).");
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
            app.MapPost("/users/register", async (RegisterUserRequest request, ISender sender, IValidator<RegisterUserRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);

                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var command = request.Adapt<RegisterUserCommand>();

                var result = await sender.Send(command);

                if (result.ErrorCarrier != null)
                {
                    return Results.Problem(detail: result.ErrorCarrier.Detail, statusCode: result.ErrorCarrier.StatusCode, title: result.ErrorCarrier.Title);
                }

                var response = result.Response.Adapt<RegisterUserResponse>();

                return Results.Ok(response);

            })
            .WithName("RegisterUser")
            .WithTags("Users")
            .AllowAnonymous();

        }
    }
}
