using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging.KafkaLogger.Configs
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "kafka:9092";
        public string Topic { get; set; } = "app-logs";
        public string Environment { get; set; } = "Docker";
        public string? ServiceName { get; set; }
        public LogLevel MinLevel { get; set; } = LogLevel.Warning;
        public int QueueCapacity { get; set; } = 1000;

        public KafkaSettings() { }

        public KafkaSettings(string serviceName)
        {
            ServiceName = serviceName;
        }

    }
}
