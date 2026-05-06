using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.Events
{
    public class BaseEvent
    {
        public Guid Id { get; init; }
        public DateTime OccurredOn { get; init; }
        public string EventType => GetType().AssemblyQualifiedName ?? GetType().Name ?? string.Empty;
        public BaseEvent()
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
        }
    }
}
