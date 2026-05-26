using ResidentialAreas.API.Grpc;
using ResidentialAreas.API.Helpers.ErrorCarrier;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AssignTenantToBuildings
{
    public record AssignTenantToBuildingCommand(long BuildingCode, string Email) : ICommand<AssignTenantToBuildingResult>;

    public record AssignTenantToBuildingResult(AssignTenantToBuildingResponse? Result, ErrorCarrier? Error);


    public class AssignTenantToBuildingsHandler : ICommandHandler<AssignTenantToBuildingCommand, AssignTenantToBuildingResult>
    {
        private readonly UserValidations.UserValidationsClient _userValidationsClient;
        private readonly AreaDbContext _areaDbContest;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AssignTenantToBuildingsHandler> _logger;

        public AssignTenantToBuildingsHandler(UserValidations.UserValidationsClient userValidationsClient, AreaDbContext areaDbContest, IHttpContextAccessor httpContextAccessor, ILogger<AssignTenantToBuildingsHandler> logger)
        {
            _userValidationsClient = userValidationsClient;
            _areaDbContest = areaDbContest;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }


        public async Task<AssignTenantToBuildingResult> Handle(AssignTenantToBuildingCommand request, CancellationToken cancellationToken)
        {
            // Extract the user ID and roles from the HTTP context
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();



            // Check if the user has the "Admin" role or the "ComplexManager" role with permission to manage the area of the building
            if (!userRoles.Contains("Admin"))
            {

                // If the user does not have the "Admin" role, check if they have the "ComplexManager" role
                if (!userRoles.Contains("ComplexManager"))
                {
                    _logger.LogWarning("Assign tenant to building failed: user {UserId} does not have Admin or ComplexManager role", userIdClaim.Value);
                    return new AssignTenantToBuildingResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to assign a tenant to a building"
                    });
                }


                // If the user has the "ComplexManager" role, check if they have permission to manage the area of the building
                var complexManagerIdOfTheAreaOfBuilding = await _areaDbContest.Buildings.AsNoTracking().Where(b => b.Code == request.BuildingCode && b.Area != null).Select(b => b.Area!.ComplexManagerId).FirstOrDefaultAsync(cancellationToken);


                // If the complex manager ID of the area of the building is null or does not match the user ID, return a forbidden error
                if (complexManagerIdOfTheAreaOfBuilding == null || complexManagerIdOfTheAreaOfBuilding != Guid.Parse(userIdClaim.Value))
                {
                    _logger.LogWarning("Assign tenant to building failed: user {UserId} is not the complex manager of building code {BuildingCode}", userIdClaim.Value, request.BuildingCode);
                    return new AssignTenantToBuildingResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to assign a tenant to this building"
                    });
                }
            }


            // Validate the user by calling the User Validations gRPC service
            GetUserResponse user = await _userValidationsClient.GetUserAsync(new GetUserRequest { Email = request.Email }, cancellationToken: cancellationToken);

            // Check if the gRPC call was successful and if the user is valid
            int statusCode = int.TryParse(user.Error?.StatusCode, out int code) ? code : 500;
            if (statusCode != 200)
            {
                _logger.LogWarning("Assign tenant to building failed: gRPC error {StatusCode} for tenant email {Email}", statusCode, request.Email);
                return new AssignTenantToBuildingResult(null, new ErrorCarrier
                {
                    Title = user.Error?.Title,
                    StatusCode = statusCode,
                    Detail = user.Error?.ErrorMessage
                });
            }



            // Check if the user is verified
            if (user.UserByRpc.IsUserVerified == false)
            {
                _logger.LogWarning("Assign tenant to building failed: tenant {Email} is not verified", request.Email);
                return new AssignTenantToBuildingResult(null, new ErrorCarrier
                {
                    Title = "USER_NOT_VERIFIED",
                    StatusCode = 403,
                    Detail = "The user must be verified to be assigned to a building"
                });
            }


            // Retrieve the building from the database using the provided building code
            Building? building = await _areaDbContest.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Code == request.BuildingCode, cancellationToken);
            if (building == null)
            {
                _logger.LogWarning("Assign tenant to building failed: no building found with code {BuildingCode}", request.BuildingCode);
                return new AssignTenantToBuildingResult(null, new ErrorCarrier
                {
                    Title = "BUILDING_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No building found with code {request.BuildingCode}"
                });
            }



            // Update the building's tenant information in the database
            try
            {
                await _areaDbContest.Buildings.Where(b => b.Id == building.Id)
                    .ExecuteUpdateAsync(b => b.SetProperty(p => p.TenantId, Guid.Parse(user.UserByRpc.Id)), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Assign tenant to building failed: database error for building code {BuildingCode} and tenant {Email}", request.BuildingCode, request.Email);
                return new AssignTenantToBuildingResult(null, new ErrorCarrier
                {
                    Title = "DATABASE_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the building's tenant information"
                });
            }

            _logger.LogInformation("Tenant {Email} assigned successfully to building code {BuildingCode}", request.Email, request.BuildingCode);
            return new AssignTenantToBuildingResult(new AssignTenantToBuildingResponse(Success : true, Message : "Tenant successfully assigned to the building"), null);

        }
    }
}
