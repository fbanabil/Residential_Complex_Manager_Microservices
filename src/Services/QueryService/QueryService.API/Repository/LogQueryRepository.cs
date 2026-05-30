using BuildingBlocks.Messaging.KafkaLogger;
using BuildingBlocks.Messaging.KafkaLogger.Configs;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace QueryService.API.Repository
{
    public class LogQueryRepository
    {
        private readonly IMongoCollection<LogModel> _collection;

        public LogQueryRepository(IMongoClient mongoClient, IOptions<MongoSettings> options)
        {
            var mongoOptions = options.Value;
            var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);
            _collection = database.GetCollection<LogModel>(mongoOptions.CollectionName);
        }

        public async Task<List<LogModel>> FilterLogsAsync(FilterDefinition<LogModel> filter, SortDefinition<LogModel> sort, int page, int pageSize, CancellationToken cancellationToken)
        {
            return await _collection
                .Find(filter)
                .Sort(sort)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> CountLogsAsync(FilterDefinition<LogModel> filter, CancellationToken cancellationToken)
        {
            return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        }

        public async Task<LogModel?> GetLogByIdAsync(string id, CancellationToken cancellationToken)
        {
            return await _collection
                .Find(Builders<LogModel>.Filter.Eq(x => x.Id, id))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<string>> GetDistinctServiceNamesAsync(CancellationToken cancellationToken)
        {
            var cursor = await _collection.DistinctAsync(
                x => x.ServiceName,
                Builders<LogModel>.Filter.Ne(x => x.ServiceName, null),
                cancellationToken: cancellationToken);

            var values = await cursor.ToListAsync(cancellationToken);
            return values.Where(v => v is not null).Select(v => v!).ToList();
        }

        public async Task<List<LogModel>> GetLogsByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken)
        {
            return await _collection
                .Find(Builders<LogModel>.Filter.Eq(x => x.CorrelationId, correlationId))
                .Sort(Builders<LogModel>.Sort.Ascending(x => x.Timestamp))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<LogModel>> GetLogsByTraceIdAsync(string traceId, CancellationToken cancellationToken)
        {
            return await _collection
                .Find(Builders<LogModel>.Filter.Eq(x => x.TraceId, traceId))
                .Sort(Builders<LogModel>.Sort.Ascending(x => x.Timestamp))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<BucketCount>> CountByLogLevelAsync(FilterDefinition<LogModel> filter, CancellationToken cancellationToken)
        {
            var groupStage = new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$LogLevel" },
                { "count", new BsonDocument("$sum", 1) }
            });
            var sortStage = new BsonDocument("$sort", new BsonDocument("count", -1));

            var results = await _collection.Aggregate()
                .Match(filter)
                .AppendStage<BsonDocument>(groupStage)
                .AppendStage<BsonDocument>(sortStage)
                .ToListAsync(cancellationToken);

            return results.Select(d => new BucketCount(
                d["_id"].IsBsonNull ? null : d["_id"].AsString,
                d["count"].ToInt64())).ToList();
        }

        public async Task<List<BucketCount>> CountByServiceNameAsync(FilterDefinition<LogModel> filter, CancellationToken cancellationToken)
        {
            var groupStage = new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$ServiceName" },
                { "count", new BsonDocument("$sum", 1) }
            });
            var sortStage = new BsonDocument("$sort", new BsonDocument("count", -1));

            var results = await _collection.Aggregate()
                .Match(filter)
                .AppendStage<BsonDocument>(groupStage)
                .AppendStage<BsonDocument>(sortStage)
                .ToListAsync(cancellationToken);

            return results.Select(d => new BucketCount(
                d["_id"].IsBsonNull ? null : d["_id"].AsString,
                d["count"].ToInt64())).ToList();
        }

        public async Task<List<TimeBucketCount>> CountByTimeBucketAsync(FilterDefinition<LogModel> filter, string bucketUnit, CancellationToken cancellationToken)
        {
            var groupStage = new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "bucket", new BsonDocument("$dateTrunc", new BsonDocument
                            {
                                { "date", "$Timestamp" },
                                { "unit", bucketUnit }
                            })
                        },
                        { "level", "$LogLevel" }
                    }
                },
                { "count", new BsonDocument("$sum", 1) }
            });
            var sortStage = new BsonDocument("$sort", new BsonDocument("_id.bucket", 1));

            var results = await _collection.Aggregate()
                .Match(filter)
                .AppendStage<BsonDocument>(groupStage)
                .AppendStage<BsonDocument>(sortStage)
                .ToListAsync(cancellationToken);

            return results.Select(d =>
            {
                var id = d["_id"].AsBsonDocument;
                return new TimeBucketCount(
                    id["bucket"].ToUniversalTime(),
                    id.Contains("level") && !id["level"].IsBsonNull ? id["level"].AsString : null,
                    d["count"].ToInt64());
            }).ToList();
        }
    }

    public record BucketCount(string? Key, long Count);

    public record TimeBucketCount(DateTime Bucket, string? LogLevel, long Count);
}
