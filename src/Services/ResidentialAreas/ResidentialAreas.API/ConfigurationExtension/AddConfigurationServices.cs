using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.Helpers.LocationValidator;
using System.Security.Cryptography;

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



            var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
            var publicKey = jwtSettingsSection.GetValue<string>("PublicKey");

            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);

            var rsaSecurityKey = new RsaSecurityKey(rsa);


            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettingsSection.GetValue<string>("Issuer"),
                    ValidAudience = jwtSettingsSection.GetValue<string>("Audience"),

                    IssuerSigningKey = rsaSecurityKey,

                    ClockSkew = TimeSpan.Zero
                };
            });
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

                options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));

                options.AddPolicy("TenantOnly", policy => policy.RequireRole("Tenant"));

                options.AddPolicy("ComplexManager", policy => policy.RequireRole("ComplexManager"));

                options.AddPolicy("UserOrTenant", policy => policy.RequireRole("User", "Tenant"));

                options.AddPolicy("AdminOrUserOrTenant", policy => policy.RequireRole("Admin", "User", "Tenant"));
            });


            builder.Services.AddScoped<IImageSaver,ImageSaver>();
            builder.Services.AddSingleton<ILocationValidator, LocationValidator>();


            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            builder.Services.AddHttpClient();

            builder.Services.AddHttpContextAccessor();


        }
    }
}
