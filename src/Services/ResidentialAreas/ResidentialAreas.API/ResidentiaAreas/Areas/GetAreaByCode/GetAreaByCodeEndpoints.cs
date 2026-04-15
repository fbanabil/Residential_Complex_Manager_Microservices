using MediatR;
using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.GetAreaByCode
{
    public record GetAreaByCodeResponse(Guid Id, long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, DateTime CreatedAt, DateTime UpdatedAt, List<string?>? ImageUrls);


    public class GetAreaByCodeRequestValidator : AbstractValidator<long>
    {
        public GetAreaByCodeRequestValidator()
        {
            RuleFor(code => code).GreaterThan(999999999).WithMessage("The area code must be a positive number.");
        }
    }


    public class GetAreaByCodeEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/areas/code/{code:long}", async (HttpContext httpContext, long code, ISender sender, [FromServices] IValidator<long> validator) =>
            {
                var validationResult = await validator.ValidateAsync(code);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.ToDictionary());
                }

                var query = new GetAreaByCodeQuery(code);
                var result = await sender.Send(query);

                if (result == null)
                {
                    return Results.NotFound($"Area with code {code} not found.");
                }

                var response = result.Adapt<GetAreaByCodeResponse>();

                string baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{baseUrl}/{url}").ToList()
                };


                return Results.Ok(response);
            })
                .WithName("GetAreaByCode")
                .WithTags("Areas")
                .Produces<GetAreaByCodeResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a residential area by its code.");
        }
    }
}
