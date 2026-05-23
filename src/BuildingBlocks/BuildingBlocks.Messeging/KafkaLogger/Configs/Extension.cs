using BuildingBlocks.Messaging.KafkaLogger.Logger;
using BuildingBlocks.Messaging.KafkaLogger.LogQueue;
using BuildingBlocks.Messaging.KafkaLogger.ProducerAndConsumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Configs
{
    public static class Extension
    {
        public static ILoggingBuilder AddKafka(this ILoggingBuilder builder, Action<KafkaSettings> configure)
        {
            builder.Services.Configure(configure);

            builder.Services.AddSingleton< BuildingBlocks.Messaging.KafkaLogger.LogQueue.LogQueue>();

            builder.Services.AddSingleton<ILogQueue>(
                sp => sp.GetRequiredService< BuildingBlocks.Messaging.KafkaLogger.LogQueue.LogQueue>());

            builder.Services.AddHostedService<KafkaLogProducer>();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, KafkaLoggerProvider>());

            return builder;

        }
    }
}
