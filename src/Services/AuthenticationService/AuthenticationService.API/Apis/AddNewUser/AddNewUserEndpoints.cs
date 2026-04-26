
namespace AuthenticationService.API.Apis.AddNewUser
{
    public class AddNewUserEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/add-new-user", () => "Hello World!").WithTags("AddNewUser")
                .AllowAnonymous();
        }
    }
}
