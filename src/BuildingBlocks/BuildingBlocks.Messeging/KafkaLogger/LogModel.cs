using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger
{
    public class LogModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? ServiceName { get; set; }
        public string? Environment { get; set; }
        public string? LogLevel { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Exception { get; set; }

        public Dictionary<string, string?> Properties { get; set; } = new(); 

        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserRoles { get; set; }


        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }

        public string? ExceptionStackTrace { get; set; }

        public string? TraceId { get; set; }

        public string? SpanId { get; set; }

        public string? RequestId { get; set; }

        public string? CorrelationId { get; set; }
        public string? Category { get; set; }
        public int EventId { get; set; }
        public string? EventName { get; set; }
    }
}
