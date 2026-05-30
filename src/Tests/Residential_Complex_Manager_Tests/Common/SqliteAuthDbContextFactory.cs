extern alias AuthApi;
using AuthApi::AuthenticationService.API.AuthenticationDbContest;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Residential_Complex_Manager_Tests.Common
{
    /// <summary>
    /// Spins up a SQLite in-memory AuthDbContext. SQLite supports transactions and the
    /// ExecuteUpdate / ExecuteDelete APIs used by the production handlers, while EF Core
    /// InMemory does not â€” that is why we don't reach for the simpler provider here.
    /// Dispose the returned context AND the connection together.
    /// </summary>
    public sealed class SqliteAuthDbContextScope : IDisposable, IAsyncDisposable
    {
        public SqliteConnection Connection { get; }
        public AuthDbContext Context { get; }

        public SqliteAuthDbContextScope()
        {
            Connection = new SqliteConnection("DataSource=:memory:");
            Connection.Open();
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(Connection)
                .EnableSensitiveDataLogging()
                .Options;
            Context = new AuthDbContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
