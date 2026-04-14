namespace ResidentialAreas.API.ConfigurationExtension
{
    public static class AddPipelineConfiguration
    {
        public static async Task AddCustomPipeline(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {

                app.UseStaticFiles();

                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<AreaDbContext>();
                    await dbContext.Database.MigrateAsync();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });

            }

            // Configure the HTTP request pipeline.
            app.MapCarter();
        }
    }
}
