using BuildingBlocks.Messaging.KafkaLogger.Configs;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.KafkaLogger.Repository
{
    public class MongoLogRepository
    {
        private readonly IMongoCollection<LogModel> _collection;

        public MongoLogRepository(IMongoClient mongoClient, IOptions<MongoSettings> options)
        {
            var mongoOptions = options.Value;

            var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
            _collection = database.GetCollection<LogModel>(mongoOptions.CollectionName);
        }


        public async Task EnsureIndexAsync(CancellationToken cancellationToken)
        {
            var indexModels = new[]
            {
                new CreateIndexModel<LogModel>(Builders<LogModel>.IndexKeys.Descending(x => x.Timestamp)),
                new CreateIndexModel<LogModel>(Builders<LogModel>.IndexKeys.Ascending(x => x.LogLevel)),
                new CreateIndexModel<LogModel>(Builders<LogModel>.IndexKeys.Ascending(x => x.ServiceName)),
                new CreateIndexModel<LogModel>(Builders<LogModel>.IndexKeys.Ascending(x => x.CorrelationId))
            };
            await _collection.Indexes.CreateManyAsync(indexModels, cancellationToken: cancellationToken);
        }

        public async Task InsertLogAsync(LogModel log, CancellationToken cancellationToken)
        {
            try
            {
                await _collection.InsertOneAsync(log, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Failed to insert log: {ex.Message}");
            }
        }
    }
}
