using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger
{
    public class LogModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? ServiceName { get; set; }
        public string? Environment { get; set; }
        public string? LogLevel { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Exception { get; set; }
        public Guid? UserID {  get; set; }
        public string? UserRole { get; set; }
        public string? UserName { get; set; }
        public Dictionary<string, string?> Details { get; set; } = new();
    }
}
