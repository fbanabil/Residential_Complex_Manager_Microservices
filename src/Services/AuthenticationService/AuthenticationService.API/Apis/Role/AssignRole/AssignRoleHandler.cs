using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Helpers.ErrorCarrier;
using CQRSPattern.CQRS;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.Apis.Role.AssignRole
{
    public record AssignRoleCommand(string UserEmail, string RoleName) : ICommand<AssignRoleResult>;
    public record AssignRoleResult(AssignRoleResponse? Result, ErrorCarrier? Error);



    public class AssignRoleHandler : ICommandHandler<AssignRoleCommand, AssignRoleResult>
    {
        private readonly AuthDbContext _authDbContext;
        private readonly ILogger<AssignRoleHandler> _logger;

        public AssignRoleHandler(AuthDbContext authDbContext, ILogger<AssignRoleHandler> logger)
        {
            _authDbContext = authDbContext;
            _logger = logger;
        }



        public async Task<AssignRoleResult> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            // Validate if user exists
            Guid? userId = await _authDbContext.Users.Where(u => u.Email == request.UserEmail).Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);
            if (userId == null || userId == Guid.Empty)
            {
                _logger.LogWarning("Assign role failed: No user found with email {Email}", request.UserEmail);
                return new AssignRoleResult(null, new ErrorCarrier()
                {
                    Title = "User Not Found",
                    StatusCode = 404,
                    Detail = $"No user found with email: {request.UserEmail}"
                });
            }



            // Validate if role exists
            EntityModels.Role? existingRole = await _authDbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == request.RoleName, cancellationToken);
            if (existingRole == null)
            {
                _logger.LogWarning("Assign role failed: No role found with name '{RoleName}'", request.RoleName);
                return new AssignRoleResult(null, new ErrorCarrier()
                {
                    Title = "Role Not Found",
                    StatusCode = 404,
                    Detail = $"No role found with name: {request.RoleName}"
                });
            }



            // Check if the user already has the role assigned
            bool alreadyAssigned = await _authDbContext.UserRoles.AsNoTracking().AnyAsync(ur => ur.UserId == userId && ur.RoleId == existingRole.Id, cancellationToken);
            if(alreadyAssigned)
            {
                _logger.LogWarning("Assign role failed: User {Email} already has role '{RoleName}'", request.UserEmail, request.RoleName);
                return new AssignRoleResult(null, new ErrorCarrier()
                {
                    Title = "Role Already Assigned",
                    StatusCode = 400,
                    Detail = $"User with email: {request.UserEmail} already has the role: {request.RoleName}"
                });
            }




            // Assign the role to the user
            EntityModels.UserRole userRole = new EntityModels.UserRole()
            {
                UserId = userId.Value,
                RoleId = existingRole.Id,
                AssignedAt = DateTime.UtcNow
            };
            await _authDbContext.UserRoles.AddAsync(userRole, cancellationToken);
            await _authDbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role '{RoleName}' assigned successfully to user with email {Email}", request.RoleName, request.UserEmail);
            return new AssignRoleResult(new AssignRoleResponse(Success:true,Message:"Role assigned successfully"), null);
        }
    }
}
