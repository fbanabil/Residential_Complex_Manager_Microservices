using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.Helpers.LocationValidator;

namespace ResidentialAreas.API.ConfigurationExtension
{
    public static class AddConfigurationServices
    {
        public static void AddCustomConfiguration(this WebApplicationBuilder builder)
        {
            builder.Services.AddCarter();

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

            });

            builder.Services.AddDbContext<AreaDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
            });


            builder.Services.AddScoped<IAreaRepository, AreaRepository>();
            builder.Services.AddScoped<IImageSaver,ImageSaver>();
            builder.Services.AddSingleton<ILocationValidator, LocationValidator>();


            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            builder.Services.AddHttpClient();


        }
    }
}
