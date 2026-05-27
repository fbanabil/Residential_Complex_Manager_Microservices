using BuildingBlocks.Messaging.KafkaLogger.Configs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using QueryService.API.Repository;
using System.Security.Cryptography;

namespace QueryService.API.ConfigurationExtension
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

            builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));

            builder.Services.AddSingleton<IMongoClient>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoSettings>>().Value;
                return new MongoClient(options.ConnectionString);
            });

            builder.Services.AddSingleton<LogQueryRepository>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "QueryService API", Version = "v1" });
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
                options.AddPolicy("ComplexManagerOnly", policy => policy.RequireRole("ComplexManager"));
                options.AddPolicy("AdminOrComplexManager", policy => policy.RequireRole("Admin", "ComplexManager"));
            });

            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        }
    }
}
