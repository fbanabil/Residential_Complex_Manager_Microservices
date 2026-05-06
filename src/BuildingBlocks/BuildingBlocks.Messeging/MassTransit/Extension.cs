using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.MassTransit
{
    public static class Extension
    {
        public static IServiceCollection AddMessageBroker(this IServiceCollection service, IConfiguration configuration, Assembly? assembly = null)
        {
            service.AddMassTransit(config =>
            {
                config.SetKebabCaseEndpointNameFormatter();

                if (assembly != null)
                {
                    config.AddConsumers(assembly);
                }
                else
                {
                    config.AddConsumers(Assembly.GetExecutingAssembly());
                }

                config.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitMqHost = configuration["RabbitMQ:Host"];
                    var rabbitMqUsername = configuration["RabbitMQ:Username"];
                    var rabbitMqPassword = configuration["RabbitMQ:Password"];

                    cfg.Host(new Uri(rabbitMqHost!), h =>
                    {
                        h.Username(rabbitMqUsername!);
                        h.Password(rabbitMqPassword!);
                    });
                    cfg.ConfigureEndpoints(context);
                    
                    cfg.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
                });

            });
            return service;
        }
    }
}
