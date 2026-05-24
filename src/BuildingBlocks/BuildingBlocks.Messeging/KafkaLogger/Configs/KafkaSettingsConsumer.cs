using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Configs
{
    public class KafkaSettingsConsumer : KafkaSettings
    {
        public string GroupId { get; set; } = "logger-group-consumer";
    }
}
