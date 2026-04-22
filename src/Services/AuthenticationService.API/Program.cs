using AuthenticationService.API.ConfigurationExtension;

var builder = WebApplication.CreateBuilder(args);

builder.AddCustomServices();


var app = builder.Build();

await app.AddCustomPipeline();

app.Run();
