using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Configs
{
    public class MongoSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    }
}
