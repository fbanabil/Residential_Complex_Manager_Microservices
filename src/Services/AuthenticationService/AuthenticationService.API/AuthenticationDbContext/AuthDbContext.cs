using AuthenticationService.API.EntityModels;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.AuthenticationDbContest
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<OAuthClient> OAuthClients => Set<OAuthClient>();
        public DbSet<OAuthScope> OAuthScopes => Set<OAuthScope>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<UserConsent> UserConsents => Set<UserConsent>();
        public DbSet<Image> Images => Set<Image>();
        public DbSet<SecurityTokens> SecurityTokens => Set<SecurityTokens>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureDb();
        }
    }
}
