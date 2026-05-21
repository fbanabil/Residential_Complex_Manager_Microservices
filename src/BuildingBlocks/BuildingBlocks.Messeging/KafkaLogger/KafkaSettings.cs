namespace BuildingBlocks.Messaging.KafkaLogger
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "9092";
        public string Topic { get; set; } = "app-logs";
        public string Environment { get; set; } = "Docker";
        public string? ServiceName { get; set; }
        public string MinLevel { get; set; } = "Information";
        public int QueueCapacity { get; set; } = 1000;


        public KafkaSettings(string serviceName)
        {
            ServiceName = serviceName;
        }

    }
}
