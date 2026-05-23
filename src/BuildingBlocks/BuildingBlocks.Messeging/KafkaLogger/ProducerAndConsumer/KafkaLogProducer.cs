using BuildingBlocks.Messaging.KafkaLogger.Configs;
using BuildingBlocks.Messaging.KafkaLogger.LogQueue;
using Confluent.Kafka;
using MassTransit.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.ProducerAndConsumer
{
    public class KafkaLogProducer : BackgroundService
    {
        private readonly LogQueue _logQueue;
        private readonly KafkaSettings _settings;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public KafkaLogProducer(LogQueue logQueue, IOptions<KafkaSettings> settings)
        {
            _logQueue = logQueue;
            _settings = settings.Value;
        }





        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                LingerMs = 5,
                MessageSendMaxRetries = 3,
                MessageTimeoutMs = 5000,
                RetryBackoffMs = 1000
            };

            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            await foreach(var logmessage in _logQueue.Reader.ReadAllAsync(stoppingToken))
            {
                var json = JsonSerializer.Serialize(logmessage, JsonOptions);
                try
                {
                   var KafkaMessage = new Message<string, string> { Key = logmessage.Id, Value = json };

                    await producer.ProduceAsync(_settings.Topic, KafkaMessage, stoppingToken);

                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine("Producer is stopping...");
                    break;
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"Failed to produce message: {ex.Error.Reason}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
            producer.Flush(TimeSpan.FromSeconds(3));
        }
    }
}
