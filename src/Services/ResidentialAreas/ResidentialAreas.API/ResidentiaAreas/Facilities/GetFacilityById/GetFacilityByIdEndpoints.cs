using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.GetFacilityById
{
    public record GetFacilityByIdResponse(Guid Id, long FacilityCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, DateTime CreatedAt, DateTime UpdatedAt, long? AreaCode, string? AreaName, long? BuildingCode, string? BuildingName, List<string?>? ImageUrls);

    public class GetFacilityByIdRequestValidator : AbstractValidator<Guid>
    {
        public GetFacilityByIdRequestValidator()
        {
            RuleFor(id => id).NotEmpty().WithMessage("The facility ID is required.");
        }
    }

    public class GetFacilityByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/facilities/{id:guid}", async (HttpContext httpContext, Guid id, ISender sender, [FromServices] IValidator<Guid> validator) =>
            {
                var validationResult = await validator.ValidateAsync(id);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.ToDictionary());
                }

                var query = new GetFacilityByIdQuery(id);
                var result = await sender.Send(query);

                if (result == null)
                {
                    return Results.NotFound($"Facility with ID {id} not found.");
                }

                var response = result.Adapt<GetFacilityByIdResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("GetFacilityById")
                .WithTags("Facilities")
                .Produces<GetFacilityByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a facility by its ID.");
        }
    }
}
