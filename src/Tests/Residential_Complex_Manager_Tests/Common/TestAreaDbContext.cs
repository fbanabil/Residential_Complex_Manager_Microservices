using Microsoft.EntityFrameworkCore;
using ResidentialAreas.API.AppDbContext;

namespace Residential_Complex_Manager_Tests.Common
{
    /// <summary>
    /// AreaDbContext seeds from JSON files inside its base directory at OnModelCreating
    /// time. That base directory is the test runner output, where the sample files don't
    /// exist — without overriding this we hit FileNotFoundException before any test code
    /// runs. We override OnModelCreating to install the production model configuration but
    /// skip the file-backed Seed() call.
    /// </summary>
    public class TestAreaDbContext : AreaDbContext
    {
        public TestAreaDbContext(DbContextOptions<AreaDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Configure();
            // Deliberately skip modelBuilder.Seed() — the JSON sample files are not copied
            // into the test output directory.
        }
    }
}
