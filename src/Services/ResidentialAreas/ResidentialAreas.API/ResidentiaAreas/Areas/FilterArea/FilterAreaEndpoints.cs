using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.FilterArea
{
    public record FilterAreaRequest(string? Name, string? City, string? State, string? Country, string? PostalCode, string? Address, string? Status);
    public record FilterAreaResponseInstance(long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string Status, List<string?>? ImageUrls);
    public record FilterAreaResponse(List<FilterAreaResponseInstance>? Areas);


    public class FilterAreaValidator : AbstractValidator<FilterAreaRequest>
    {

        public FilterAreaValidator()
        {
            RuleFor(x => x.Status).IsEnumName(typeof(Status)).When(x => !string.IsNullOrEmpty(x.Status)).WithMessage("The status must be a valid value (Active, Inactive, Maintenance).");
        }
    }



    public class FilterAreaEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/areas/filter", async (HttpContext httpContext, [AsParameters] FilterAreaRequest request, ISender sender, [FromServices] IValidator<FilterAreaRequest> validator) =>
            {
                var validationResult = await validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var query = request.Adapt<FilterAreaQuery>();
                var result = await sender.Send(query);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    return Results.Problem(detail: result.ErrorMessage);
                }


                if (result.Areas == null || !result.Areas.Any())
                {
                    return Results.Ok(new FilterAreaResponse(new List<FilterAreaResponseInstance>()));
                }

                var response = new FilterAreaResponse(result.Areas.Select(area => area.Adapt<FilterAreaResponseInstance>() with { ImageUrls = area.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList() }).ToList());

                return Results.Ok(response);
            })
                .WithName("FilterAreas")
                .WithTags("Areas")
                .Produces<FilterAreaResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Filters residential areas based on the provided criteria as parameters.");
        }
    }
}
