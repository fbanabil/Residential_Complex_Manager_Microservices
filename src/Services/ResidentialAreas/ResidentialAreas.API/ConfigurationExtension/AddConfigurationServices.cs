using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.Helpers.LocationValidator;
using System.Security.Cryptography;
using ResidentialAreas.API.Grpc;

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

                options.AddPolicy("ComplexManagerOnly", policy => policy.RequireRole("ComplexManager"));

                options.AddPolicy("TenantOnly", policy => policy.RequireRole("Tenant"));

                options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));

                options.AddPolicy("AdminOrComplexManager", policy => policy.RequireRole("Admin", "ComplexManager"));

                options.AddPolicy("AdminOrComplexManagerOrTenant", policy => policy.RequireRole("Admin", "ComplexManager", "Tenant"));

            });

            builder.Services.AddGrpcClient<ResidentialAreas.API.Grpc.UserValidations.UserValidationsClient>(options =>
            {
                options.Address = new Uri(builder.Configuration.GetValue<string>("GrpcSettings:UserValidationServiceUrl")!);
            });
            //.ConfigurePrimaryHttpMessageHandler(() =>
            //{
            //    var handler = new SocketsHttpHandler
            //    {
            //        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            //        {
            //            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            //            {
            //                if (builder.Environment.IsDevelopment())
            //                {
            //                    return true;
            //                }
            //                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            //            }
            //        }
            //    };
            //    return handler;
            //});



            builder.Services.AddScoped<IImageSaver,ImageSaver>();
            builder.Services.AddSingleton<ILocationValidator, LocationValidator>();


            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

            builder.Services.AddHttpClient();

            builder.Services.AddHttpContextAccessor();


        }
    }
}
