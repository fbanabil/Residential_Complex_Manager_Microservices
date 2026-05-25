using BuildingBlocks.Messaging.KafkaLogger.Logger;
using BuildingBlocks.Messaging.KafkaLogger.LogQueue;
using BuildingBlocks.Messaging.KafkaLogger.ProducerAndConsumer;
using BuildingBlocks.Messaging.KafkaLogger.Repository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Configs
{
    public static class Extension
    {
        public static ILoggingBuilder AddKafka(this ILoggingBuilder builder, Action<KafkaSettings> configure)
        {

            builder.ClearProviders();
            builder.AddConsole(); 

            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
            


            builder.AddFilter<KafkaLoggerProvider>((category, logLevel) =>
            {
                if (category == null)
                {
                    return logLevel >= LogLevel.Information;
                }

                var suppressedPrefixes = new[]
                {
                    "Microsoft",
                    "System",
                    "MassTransit",
                    "LuckyPennySoftware",
                    "RabbitMQ"
                };

                if (suppressedPrefixes.Any(prefix => category.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    return logLevel >= LogLevel.Error;
                }

                return logLevel >= LogLevel.Information;
            });


            builder.Services.Configure(configure);

            builder.Services.AddSingleton< BuildingBlocks.Messaging.KafkaLogger.LogQueue.LogQueue>();

            builder.Services.AddSingleton<ILogQueue>(
                sp => sp.GetRequiredService< BuildingBlocks.Messaging.KafkaLogger.LogQueue.LogQueue>());

            builder.Services.AddHostedService<KafkaLogProducer>();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, KafkaLoggerProvider>());


            builder.Services.AddSingleton<IMongoClient>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoSettings>>().Value;
                return new MongoClient(options.ConnectionString);
            });
            builder.Services.AddSingleton<MongoLogRepository>();
            builder.Services.AddHostedService<KafkaLogConsumer>();

            return builder;

        }
    }
}
