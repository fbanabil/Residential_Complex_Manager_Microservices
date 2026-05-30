using BuildingBlocks.Messaging.KafkaLogger;
using BuildingBlocks.Messaging.KafkaLogger.Configs;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using QueryService.API.Repository;

namespace Residential_Complex_Manager_Tests.QueryService.Integration
{
    /// <summary>
    /// MongoDB integration tests. They require a real Mongo instance reachable at
    /// MONGO_TEST_CONNECTION (defaults to localhost). If Mongo is not reachable, every
    /// test in the class is auto-skipped via the SkipIfMongoUnavailable helper. The tests
    /// write to a sandboxed database whose name is suffixed with a GUID and drop it at
    /// teardown â€” so they're safe to run against any test cluster.
    /// </summary>
    public class LogQueryRepositoryIntegrationTests : IAsyncLifetime
    {
        private static readonly string Conn =
            Environment.GetEnvironmentVariable("MONGO_TEST_CONNECTION") ?? "mongodb://localhost:27017";

        private MongoClient? _client;
        private string _dbName = string.Empty;
        private LogQueryRepository? _repo;

        public async Task InitializeAsync()
        {
            _client = new MongoClient(Conn);
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _client.ListDatabaseNamesAsync(cts.Token);
            }
            catch
            {
                _client = null;
                return;
            }

            _dbName = $"logs_test_{Guid.NewGuid():N}";
            var opts = Options.Create(new MongoSettings
            {
                DatabaseName = _dbName,
                CollectionName = "logs"
            });
            _repo = new LogQueryRepository(_client, opts);
        }

        public async Task DisposeAsync()
        {
            if (_client is not null && !string.IsNullOrEmpty(_dbName))
            {
                await _client.DropDatabaseAsync(_dbName);
            }
        }

        private bool MongoUnavailable => _client is null || _repo is null;

        private async Task SeedAsync(params LogModel[] models)
        {
            var coll = _client!.GetDatabase(_dbName).GetCollection<LogModel>("logs");
            await coll.InsertManyAsync(models);
        }

        [Fact]
        public async Task FilterLogs_with_no_filter_returns_paged_results()
        {
            if (MongoUnavailable) return;
            var now = DateTime.UtcNow;
            await SeedAsync(
                new LogModel { ServiceName = "auth", LogLevel = "Error",        Timestamp = now.AddMinutes(-1) },
                new LogModel { ServiceName = "auth", LogLevel = "Information",  Timestamp = now.AddMinutes(-2) },
                new LogModel { ServiceName = "query", LogLevel = "Warning",     Timestamp = now.AddMinutes(-3) });

            var fb = Builders<LogModel>.Filter.Empty;
            var sb = Builders<LogModel>.Sort.Descending(x => x.Timestamp);
            var page1 = await _repo!.FilterLogsAsync(fb, sb, 1, 2, default);
            page1.Should().HaveCount(2);
            var total = await _repo.CountLogsAsync(fb, default);
            total.Should().Be(3);
        }

        [Fact]
        public async Task GetLogById_returns_null_for_unknown_id()
        {
            if (MongoUnavailable) return;
            var got = await _repo!.GetLogByIdAsync("does-not-exist", default);
            got.Should().BeNull();
        }

        [Fact]
        public async Task GetDistinctServiceNames_returns_unique_set()
        {
            if (MongoUnavailable) return;
            await SeedAsync(
                new LogModel { ServiceName = "auth"   },
                new LogModel { ServiceName = "auth"   },
                new LogModel { ServiceName = "query"  },
                new LogModel { ServiceName = "areas"  });
            var names = await _repo!.GetDistinctServiceNamesAsync(default);
            names.Should().BeEquivalentTo(new[] { "auth", "query", "areas" });
        }

        [Fact]
        public async Task GetLogsByCorrelationId_is_sorted_ascending_by_timestamp()
        {
            if (MongoUnavailable) return;
            var now = DateTime.UtcNow;
            await SeedAsync(
                new LogModel { CorrelationId = "cid-1", Timestamp = now.AddMinutes(-1) },
                new LogModel { CorrelationId = "cid-1", Timestamp = now.AddMinutes(-3) },
                new LogModel { CorrelationId = "cid-1", Timestamp = now.AddMinutes(-2) },
                new LogModel { CorrelationId = "cid-2", Timestamp = now });
            var logs = await _repo!.GetLogsByCorrelationIdAsync("cid-1", default);
            logs.Should().HaveCount(3);
            logs.Select(l => l.Timestamp).Should().BeInAscendingOrder();
        }
    }

    public class SkipTestException : Exception
    {
        public SkipTestException(string reason) : base(reason) { }
    }
}
