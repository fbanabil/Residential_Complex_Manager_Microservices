using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Helpers.Authorization;
using Carter;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ResidentialAreas.API.Helpers.ImageSaver;

namespace AuthenticationService.API.ConfigurationExtension
{
    public static class AddServicesConfigurationExtensions
    {
        public static void AddCustomServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddCarter();
            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            });

            builder.Services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });


            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddGrpc();


            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
            });
            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSingleton<IImageSaver, ImageSaver>();
            builder.Services.AddSingleton<IAuthorizationTokenCreator, AuthorizationTokenCreator>();

        }
    }
}
