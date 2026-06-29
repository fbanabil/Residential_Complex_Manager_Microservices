var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/logs/swagger/v1/swagger.json", "Logs / QueryService");
    c.SwaggerEndpoint("/residential-areas/swagger/v1/swagger.json", "Residential Areas");
});

app.UseHttpsRedirection();
app.UseHsts();


app.MapReverseProxy();



app.Run();
