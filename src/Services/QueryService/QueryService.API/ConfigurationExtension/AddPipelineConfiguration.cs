namespace QueryService.API.ConfigurationExtension
{
    public static class AddPipelineConfiguration
    {
        public static void AddCustomPipeline(this WebApplication app)
        {
            app.UseHsts();
            app.UseHttpsRedirection();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "QueryService API V1");
                });
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapCarter();
        }
    }
}
