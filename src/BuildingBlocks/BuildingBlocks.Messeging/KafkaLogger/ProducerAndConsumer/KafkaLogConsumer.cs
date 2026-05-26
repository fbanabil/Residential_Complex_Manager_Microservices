using BuildingBlocks.Messaging.KafkaLogger.Configs;
using BuildingBlocks.Messaging.KafkaLogger.Repository;
using DnsClient.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.ProducerAndConsumer
{
    public sealed class KafkaLogConsumer : BackgroundService
    {
        private readonly KafkaSettingsConsumer _options;
        private MongoLogRepository _repository;
        private readonly ILogger<KafkaLogConsumer> _logger;
        private readonly IConfiguration _config;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public KafkaLogConsumer(IOptions<KafkaSettingsConsumer> options, MongoLogRepository mongoLogRepository, ILogger<KafkaLogConsumer> logger, IConfiguration config)
        {
            _options = options.Value;
            _repository = mongoLogRepository;
            _logger = logger;
            _config = config;
        }



        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _repository.EnsureIndexAsync(stoppingToken);

            var consumerConfig = new Confluent.Kafka.ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                GroupId = _options.GroupId,
                AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                AllowAutoCreateTopics = true
            };  


            using var consumer = new Confluent.Kafka.ConsumerBuilder<string, string>(consumerConfig).SetErrorHandler((_, e) =>
            {
                _logger.LogError($"Kafka Consumer error: {e.Reason}");
            }).Build();

            consumer.Subscribe(_options.Topic);

            _logger.LogInformation("Kafka Log Consumer started, listening to topic: {Topic} for service: {Service}", _options.Topic, _config.GetValue<string>("Service:Name"));



            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);
                        if (consumeResult != null)
                        {
                            var logMessage = consumeResult.Message.Value;
                            var logModel = JsonSerializer.Deserialize<LogModel>(logMessage, JsonOptions);
                            if (logModel != null)
                            {
                                await _repository.InsertLogAsync(logModel, stoppingToken);
                                consumer.Commit(consumeResult);
                            }
                            else
                            {
                                _logger.LogWarning("Received invalid log message: {Message}", logMessage);
                            }
                        }
                    }
                    catch (Confluent.Kafka.ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error consuming Kafka message");
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Error deserializing log message");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                consumer.Close();
                _logger.LogInformation("Kafka Log Consumer stopped.");
            }
        }
    }
}
