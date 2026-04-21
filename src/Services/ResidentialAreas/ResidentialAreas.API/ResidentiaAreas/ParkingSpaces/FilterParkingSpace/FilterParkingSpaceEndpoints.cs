using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Mapster;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.FilterParkingSpace
{
    public record FilterParkingSpaceRequest(long? AreaCode, string? Name, string? BlockNo, string? Status);
    public record FilterParkingSpaceResponseInstance(Guid Id, long ParkingSpaceCode, string Name, string? Description, string BlockNo, string Status, long AreaCode, string AreaName, List<string?>? ImageUrls);
    public record FilterParkingSpaceResponse(List<FilterParkingSpaceResponseInstance>? ParkingSpaces);

    public class FilterParkingSpaceValidator : AbstractValidator<FilterParkingSpaceRequest>
    {
        public FilterParkingSpaceValidator()
        {
            RuleFor(x => x.AreaCode).GreaterThanOrEqualTo(1000000000).When(x => x.AreaCode.HasValue).WithMessage("Area code must be greater than or equal to 1000000000.");
            RuleFor(x => x.AreaCode).LessThan(2000000000).When(x => x.AreaCode.HasValue).WithMessage("Area code must be less than 2000000000.");
            RuleFor(x => x.Status).IsEnumName(typeof(Status)).When(x => !string.IsNullOrWhiteSpace(x.Status)).WithMessage("Status must be a valid value (Active, Inactive, Maintenance).");
        }
    }

    public class FilterParkingSpaceEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/parking-spaces/filter", async (HttpContext httpContext, [AsParameters] FilterParkingSpaceRequest request, ISender sender, [FromServices] IValidator<FilterParkingSpaceRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = request.Adapt<FilterParkingSpaceQuery>();
                var result = await sender.Send(query);

                if (result.ParkingSpaces == null || !result.ParkingSpaces.Any())
                {
                    return Results.Ok(new FilterParkingSpaceResponse(new List<FilterParkingSpaceResponseInstance>()));
                }

                var response = new FilterParkingSpaceResponse(result.ParkingSpaces.Select(parkingSpace => parkingSpace with
                {
                    ImageUrls = parkingSpace.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                }).ToList());

                return Results.Ok(response);
            })
                .WithName("FilterParkingSpaces")
                .WithTags("ParkingSpaces")
                .Produces<FilterParkingSpaceResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Filters parking spaces based on provided criteria.");
        }
    }
}
