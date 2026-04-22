using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.AuthenticationDbContest
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureDb();
        }
    }
}
