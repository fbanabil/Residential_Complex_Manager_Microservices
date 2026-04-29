using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Helpers.ErrorCarrier;
using CQRSPattern.CQRS;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.Apis.Role
{
    public record AddNewRoleCommand(string Name, string Description) : ICommand<AddNewRoleResult>;
    public record AddNewRoleResult(AddNewRoleResponse? Result, ErrorCarrier? ErrorCarrier);
    public class AddNewRoleHandler : ICommandHandler<AddNewRoleCommand, AddNewRoleResult>
    {
        private readonly AuthDbContext _authDbContext;
        public AddNewRoleHandler(AuthDbContext authDbContext)
        {
            _authDbContext = authDbContext;
        }
        public async Task<AddNewRoleResult> Handle(AddNewRoleCommand request, CancellationToken cancellationToken)
        {
            EntityModels.Role? existingRole = await _authDbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == request.Name, cancellationToken);


            if (existingRole!=null)
            {
                ErrorCarrier errorCarrier = new ErrorCarrier
                {
                    Title = "ROLE_ALREADY_EXISTS",
                    StatusCode = 400,
                    Detail = $"A role with the name '{request.Name}' already exists."
                };
                return new AddNewRoleResult(null, errorCarrier);
            }

            EntityModels.Role newRole = new EntityModels.Role
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _authDbContext.Roles.AddAsync(newRole, cancellationToken);
                await _authDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                ErrorCarrier errorCarrier = new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while saving the new role to the database."
                };
                return new AddNewRoleResult(null, errorCarrier);
            }

            var response = newRole.Adapt<AddNewRoleResponse>();

            return new AddNewRoleResult(response, null);
        }
    }
}
