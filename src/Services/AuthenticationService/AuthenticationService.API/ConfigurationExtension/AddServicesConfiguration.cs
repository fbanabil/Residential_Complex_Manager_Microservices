using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Helpers.Authenticate;
using Carter;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ResidentialAreas.API.Helpers.ImageSaver;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AuthenticationService.API.Helpers.Email;
using AuthenticationService.API.Helpers.GetHostUrl;
using AuthenticationService.API.Helpers.VerificationToken;
using AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using Microsoft.AspNetCore.Authentication.Cookies;
using AuthenticationService.API.HostService;
using BuildingBlocks.Messaging.MassTransit;

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



            builder.Services.AddHostedService<AddAdminHostService>();


            var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
            var publicKey = jwtSettingsSection.GetValue<string>("PublicKey");

            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);

            var rsaSecurityKey = new RsaSecurityKey(rsa);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
                        IssuerSigningKey = rsaSecurityKey
                    };
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddGoogle(options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

                    options.Scope.Add("email");
                    options.Scope.Add("profile");

                    options.SaveTokens = true;

                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

                options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));

                options.AddPolicy("TenantOnly", policy => policy.RequireRole("Tenant"));

                options.AddPolicy("UserOrTenant", policy => policy.RequireRole("User", "Tenant"));

                options.AddPolicy("AdminOrUserOrTenant", policy => policy.RequireRole("Admin", "User", "Tenant"));
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
            builder.Services.AddSingleton<IAuthenticationTokenCreator, AuthenticationTokenCreator>();
            builder.Services.AddScoped<IEmailHelper, EmailHelper>();
            builder.Services.AddScoped<IGetHostUrl, GetHostUrl>();
            builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<IVerificationTokenGenerator, VerificationTokenGenerator>();


            var scope = builder.Services.BuildServiceProvider().CreateScope();

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            builder.Services.AddMessageBroker(configuration);

        }
    }
}
