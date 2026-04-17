using Microsoft.AspNetCore.Mvc;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.GetBuildingByCode
{
    public record GetBuildingByCodeResponse(Guid Id, long Code, string Name, string BlockNo, int? TotalFloors, string Address, string Status, DateTime CreatedAt, DateTime UpdatedAt, long AreaCode, string AreaName, List<string?>? ImageUrls);

    public class GetBuildingByCodeRequestValidator : AbstractValidator<long>
    {
        public GetBuildingByCodeRequestValidator()
        {
            RuleFor(code => code)
                .GreaterThanOrEqualTo(2000000000).WithMessage("The building code must be greater than or equal to 2000000000.")
                .LessThan(3000000000).WithMessage("The building code must be less than 3000000000.");
        }
    }

    public class GetBuildingByCodeEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/buildings/code/{code:long}", async (HttpContext httpContext, long code, ISender sender, [FromServices] IValidator<long> validator) =>
            {
                var validationResult = await validator.ValidateAsync(code);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.ToDictionary());
                }

                var query = new GetBuildingByCodeQuery(code);
                var result = await sender.Send(query);

                if (result == null)
                {
                    return Results.NotFound($"Building with code {code} not found.");
                }

                var response = result.Adapt<GetBuildingByCodeResponse>();
                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("GetBuildingByCode")
                .WithTags("Buildings")
                .Produces<GetBuildingByCodeResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a building by its code.");
        }
    }
}