
using Microsoft.AspNetCore.Mvc;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.GetAreaById
{
    public record GetAreaByIdResponse(Guid Id,long Code, string Name, string City, string State, string Country, string PostalCode, string Address, string GeoBoundary, string Status, DateTime CreatedAt, DateTime UpdatedAt, List<string?>? ImageUrls);


    public class GetAreaByIdRequestValidator : AbstractValidator<Guid>
    {
        public GetAreaByIdRequestValidator()
        {
            RuleFor(id => id).NotEmpty().WithMessage("The area ID is required.");
        }
    }



    public class GetAreaByIdEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/areas/{id:guid}", async (HttpContext httpContext, Guid id, ISender sender, [FromServices] IValidator<Guid> validator) =>
            {
                var validationResult = await validator.ValidateAsync(id);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(validationResult.ToDictionary());
                }

                var query = new GetAreaByIdQuery(id);
                var result = await sender.Send(query);
                if (result == null)
                {
                    return Results.NotFound($"Area with ID {id} not found.");
                }
                var response = result.Adapt<GetAreaByIdResponse>();

                response = response with
                {
                    ImageUrls = response.ImageUrls?.Select(url => $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{url}").ToList()
                };

                return Results.Ok(response);
            })
                .WithName("GetAreaById")
                .WithTags("Areas")
                .Produces<GetAreaByIdResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Gets a residential area by its ID.");
        }
    }
}
