using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Configs
{
    public class MongoSettings
    {
        public string ConnectionString { get; set; } = "mongodb://admin:secret@mongodb:27017/loggerdb?authSource=admin";
        public string DatabaseName { get; set; } = "loggerdb";
        public string CollectionName { get; set; } = "logs";
        
    }
}
