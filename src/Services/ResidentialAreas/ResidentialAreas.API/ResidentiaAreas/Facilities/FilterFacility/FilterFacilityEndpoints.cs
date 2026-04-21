using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.FilterFacility
{
    public record FilterFacilityRequest(long? AreaCode, long? BuildingCode, string? Name, string? FacilityType, int? Capacity, bool? BookingRequired, string? Status);
    public record FilterFacilityResponseInstance(Guid Id, long FacilityCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, long? AreaCode, string? AreaName, long? BuildingCode, string? BuildingName, List<string?>? ImageUrls);
    public record FilterFacilityResponse(List<FilterFacilityResponseInstance>? Facilities);

    public class FilterFacilityValidator : AbstractValidator<FilterFacilityRequest>
    {
        public FilterFacilityValidator()
        {
            RuleFor(x => x.AreaCode)
                .GreaterThanOrEqualTo(1000000000)
                .When(x => x.AreaCode.HasValue)
                .WithMessage("Area code must be greater than or equal to 1000000000.");

            RuleFor(x => x.AreaCode)
                .LessThan(2000000000)
                .When(x => x.AreaCode.HasValue)
                .WithMessage("Area code must be less than 2000000000.");

            RuleFor(x => x.BuildingCode)
                .GreaterThanOrEqualTo(2000000000)
                .When(x => x.BuildingCode.HasValue)
                .WithMessage("Building code must be greater than or equal to 2000000000.");

            RuleFor(x => x.BuildingCode)
                .LessThan(3000000000)
                .When(x => x.BuildingCode.HasValue)
                .WithMessage("Building code must be less than 3000000000.");

            RuleFor(x => x.Capacity)
                .GreaterThan(0)
                .When(x => x.Capacity.HasValue)
                .WithMessage("Capacity must be greater than 0.");

            RuleFor(x => x.Status)
                .IsEnumName(typeof(Status))
                .When(x => !string.IsNullOrWhiteSpace(x.Status))
                .WithMessage("Status must be a valid value (Active, Inactive, Maintenance).");
        }
    }

    public class FilterFacilityEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/facilities/filter", async (HttpContext httpContext, [AsParameters] FilterFacilityRequest request, ISender sender, [FromServices] IValidator<FilterFacilityRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = request.Adapt<FilterFacilityQuery>();
                var result = await sender.Send(query);

                if (result.Facilities == null || !result.Facilities.Any())
                {
                    return Results.Ok(new FilterFacilityResponse(new List<FilterFacilityResponseInstance>()));
                }

                var response = new FilterFacilityResponse(result.Facilities.Select(facility => facility with
                {
                    ImageUrls = facility.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                }).ToList());

                return Results.Ok(response);
            })
                .WithName("FilterFacilities")
                .WithTags("Facilities")
                .Produces<FilterFacilityResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Filters facilities based on provided criteria.");
        }
    }
}
