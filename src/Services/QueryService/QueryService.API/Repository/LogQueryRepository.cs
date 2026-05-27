using BuildingBlocks.Messaging.KafkaLogger;
using BuildingBlocks.Messaging.KafkaLogger.Configs;
using Microsoft.Extensions.Options;
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
            return await _collection
                .DistinctAsync(x => x.ServiceName, Builders<LogModel>.Filter.Ne(x => x.ServiceName, null), cancellationToken: cancellationToken)
                .ContinueWith(t => t.Result.ToList(), cancellationToken);
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
    }
}
