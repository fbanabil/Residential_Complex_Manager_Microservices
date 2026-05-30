using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ResidentialAreas.API.AppDbContext;

namespace Residential_Complex_Manager_Tests.Common
{
    public sealed class SqliteAreaDbContextScope : IDisposable, IAsyncDisposable
    {
        public SqliteConnection Connection { get; }
        public AreaDbContext Context { get; }

        public SqliteAreaDbContextScope()
        {
            Connection = new SqliteConnection("DataSource=:memory:");
            Connection.Open();
            var options = new DbContextOptionsBuilder<AreaDbContext>()
                .UseSqlite(Connection)
                .EnableSensitiveDataLogging()
                .Options;
            // Use the testable subclass — see TestAreaDbContext for why we cannot use the
            // production AreaDbContext directly in tests.
            Context = new TestAreaDbContext(options);
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
