extern alias AuthApi;
using AuthApi::AuthenticationService.API.EntityModels;
using AuthApi::AuthenticationService.API.Enum;
using Microsoft.EntityFrameworkCore;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Integration
{
    /// <summary>
    /// Integration tests verifying the AuthDbContext configuration: unique indexes, default
    /// columns, FK relationships, and navigation behaviour against a real (SQLite) provider.
    /// </summary>
    public class AuthDbContextIntegrationTests
    {
        [Fact]
        public async Task Users_email_index_is_unique()
        {
            await using var s = new SqliteAuthDbContextScope();
            var ctx = s.Context;
            ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "dup@x.com", Username = "u1",
                PasswordHash = "h", Status = Status.Active, AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            ctx.Users.Add(new User { Id = Guid.NewGuid(), Email = "dup@x.com", Username = "u2",
                PasswordHash = "h", Status = Status.Active, AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

            var act = async () => await ctx.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task UserRoles_composite_PK_prevents_double_assignment()
        {
            await using var s = new SqliteAuthDbContextScope();
            var ctx = s.Context;
            var role = new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "A", CreatedAt = DateTime.UtcNow };
            var user = new User { Id = Guid.NewGuid(), Email = "a@x.com", Username = "a",
                PasswordHash = "h", Status = Status.Active, AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            ctx.Roles.Add(role);
            ctx.Users.Add(user);
            ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            // EF tracks the same key, so adding a duplicate UserRole throws even before
            // SaveChanges is called. We tolerate either path (InvalidOperationException
            // from EF's identity map or DbUpdateException from the DB).
            try
            {
                ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
                var act = async () => await ctx.SaveChangesAsync();
                await act.Should().ThrowAsync<Exception>();
            }
            catch (InvalidOperationException)
            {
                // EF identity-map rejection — also acceptable proof of the constraint.
            }
        }

        [Fact]
        public async Task Querying_user_with_includes_returns_navigation_properties()
        {
            await using var s = new SqliteAuthDbContextScope();
            var ctx = s.Context;
            var role = new Role { Id = Guid.NewGuid(), Name = "User", Description = "U", CreatedAt = DateTime.UtcNow };
            var user = new User { Id = Guid.NewGuid(), Email = "x@x.com", Username = "x",
                PasswordHash = "h", Status = Status.Active, AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            ctx.Roles.Add(role);
            ctx.Users.Add(user);
            ctx.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var loaded = await ctx.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .SingleAsync(u => u.Email == "x@x.com");
            loaded.UserRoles.Should().ContainSingle();
            loaded.UserRoles.Single().Role!.Name.Should().Be("User");
        }
    }
}
