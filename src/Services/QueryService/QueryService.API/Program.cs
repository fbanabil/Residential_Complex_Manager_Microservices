using QueryService.API.ConfigurationExtension;

var builder = WebApplication.CreateBuilder(args);

builder.AddCustomConfiguration();

var app = builder.Build();

app.AddCustomPipeline();

app.Run();
