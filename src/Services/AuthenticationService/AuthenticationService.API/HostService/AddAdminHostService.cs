using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.EntityModels;
using AuthenticationService.API.Enum;
using AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.HostService
{
    public class AddAdminHostService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;

        public AddAdminHostService(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _configuration = configuration;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {

            // Create a new scope to get scoped services like DbContext and IPasswordHasher
            using var scope = _serviceScopeFactory.CreateScope();
            var _authDbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var _passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();



            // Check if the admin user already exists based on the email from configuration
            EntityModels.User? adminUser = await _authDbContext.Users.FirstOrDefaultAsync(u => u.Email == _configuration["Admin:Email"], cancellationToken);
            if (adminUser == null)
            {
                // If the admin user does not exist, create a new one with the details from configuration
                adminUser = new EntityModels.User
                {
                    Id = Guid.NewGuid(),
                    Username = _configuration["Admin:Username"] ?? "admin",
                    Email = _configuration["Admin:Email"] ?? "xyz@gmail.com",
                    PasswordHash = await _passwordHasher.HashPassword(_configuration["Admin:Password"] ?? "Admin@123"),
                    Phone = _configuration["Admin:PhoneNumber"] ?? "01700000000",
                    Status = Status.Active,
                    IsUserVerified = true,
                    IsEmailVerified = true,
                    AuthProvider = Enum.AuthProvider.Local,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastLoginAt = null,
                    ProfileImageId = null,
                    NidImageId = null
                };


                await _authDbContext.Users.AddAsync(adminUser, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // If the admin user already exists, we can optionally update their details to ensure they have the correct information and permissions
                string newPasswordHash = await _passwordHasher.HashPassword(_configuration["Admin:Password"] ?? "Admin@123");
                await _authDbContext.Users.Where(u => u.Id == adminUser.Id)
                    .ExecuteUpdateAsync(updates => updates
                        .SetProperty(u => u.PasswordHash, newPasswordHash)
                        .SetProperty(u => u.Phone, _configuration["Admin:PhoneNumber"])
                        .SetProperty(u => u.Status, Status.Active)
                        .SetProperty(u => u.IsUserVerified, true)
                        .SetProperty(u => u.IsEmailVerified, true)
                        .SetProperty(u => u.AuthProvider, Enum.AuthProvider.Local)
                        .SetProperty(u => u.UpdatedAt, DateTime.UtcNow), cancellationToken);
            }




            // Check if the "Admin" role already exists
            Role? existingAdminRole = await _authDbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);
            if (existingAdminRole == null)
            {
                // If the "Admin" role does not exist, create it
                existingAdminRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    Description = "Administrator role with full permissions",
                    CreatedAt = DateTime.UtcNow
                };
                await _authDbContext.Roles.AddAsync(existingAdminRole, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }




            // Check if the admin user already has the "Admin" role assigned
            UserRole? existingUserRole = await _authDbContext.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == existingAdminRole.Id, cancellationToken);
            if (existingUserRole == null)
            {
                // If the admin user does not have the "Admin" role assigned, assign it to them
                existingUserRole = new UserRole()
                {
                    UserId = adminUser.Id,
                    RoleId = existingAdminRole.Id,
                    AssignedAt = DateTime.UtcNow
                };

                await _authDbContext.UserRoles.AddAsync(existingUserRole, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }




        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
