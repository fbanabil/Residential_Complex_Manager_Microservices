using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Mapster;
using MediatR;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.GetParkingSpaceById
{
    public record GetParkingSpaceByIdResponse(Guid Id, long ParkingSpaceCode, string Name, string? Description, string BlockNo, string Status, DateTime CreatedAt, DateTime UpdatedAt, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class GetParkingSpaceByIdRequestValidator : AbstractValidator<Guid>
    {
        public GetParkingSpaceByIdRequestValidator()
        {
            RuleFor(id => id).NotEmpty().WithMessage("The parking space ID is required.");
        }
    }

    public class GetParkingSpaceByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/parking-spaces/{id:guid}", async (HttpContext httpContext, Guid id, ISender sender, [FromServices] IValidator<Guid> validator) =>
            {
                var validationResult = await validator.ValidateAsync(id);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.ToDictionary());
                }

                var query = new GetParkingSpaceByIdQuery(id);
                var result = await sender.Send(query);
                if (result == null)
                {
                    return Results.NotFound($"Parking space with ID {id} not found.");
                }

                var response = result.Adapt<GetParkingSpaceByIdResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("GetParkingSpaceById")
                .WithTags("ParkingSpaces")
                .Produces<GetParkingSpaceByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a parking space by its ID.");
        }
    }
}
