using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Grpc.Services;
using Carter;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.ConfigurationExtension
{
    public static class AddPipelineConfiguration
    {
        public static async Task AddCustomPipeline(this WebApplication app)
        {
            app.UseStaticFiles();
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<AuthDbContext>();
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

            app.MapGrpcService<GreeterService>();
            app.MapCarter();

            
        }
    }
}
