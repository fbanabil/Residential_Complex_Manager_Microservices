using ResidentialAreas.API.ConfigurationExtension;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.AddCustomConfiguration();

var app = builder.Build();

//Configure the HTTP request pipeline.

app.AddCustomPipeline().Wait();

app.Run();
