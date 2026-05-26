using ResidentialAreas.API.Helpers.ErrorCarrier;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AssignFacilityToBuilding
{
    public record AssignFacilityToBuildingCommand(long FacilityCode, long BuildingCode) : ICommand<AssignFacilityToBuildingResult>;
    public record AssignFacilityToBuildingResult(AssignFacilityToBuildingResponse? Result, ErrorCarrier? Error);

    public class AssignFacilityToBuildingHandler : ICommandHandler<AssignFacilityToBuildingCommand, AssignFacilityToBuildingResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AssignFacilityToBuildingHandler> _logger;

        public AssignFacilityToBuildingHandler(AreaDbContext areaDbContext, IHttpContextAccessor httpContextAccessor, ILogger<AssignFacilityToBuildingHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AssignFacilityToBuildingResult> Handle(AssignFacilityToBuildingCommand request, CancellationToken cancellationToken)
        {
            // Validate Building
            Building? building = await _areaDbContext.Buildings.AsNoTracking().Include(x => x.Area).FirstOrDefaultAsync(b => b.Code == request.BuildingCode, cancellationToken);
            if (building == null)
            {
                _logger.LogWarning("Assign facility to building failed: no building found with code {BuildingCode}", request.BuildingCode);
                return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                {
                    Title = "BUILDING_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No building found with code {request.BuildingCode}."
                });
            }




            // Validate Facility
            Facility? facility = await _areaDbContext.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.FacilityCode == request.FacilityCode, cancellationToken);
            if (facility == null)
            {
                _logger.LogWarning("Assign facility to building failed: no facility found with code {FacilityCode}", request.FacilityCode);
                return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                {
                    Title = "FACILITY_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No facility found with code {request.FacilityCode}."
                });
            }




            // Validate Intigrity
            if (facility.BuildingId == building.Id)
            {
                _logger.LogWarning("Assign facility to building failed: facility code {FacilityCode} already assigned to building code {BuildingCode}", request.FacilityCode, request.BuildingCode);
                return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                {
                    Title = "ALREADY_ASSIGNED",
                    StatusCode = 400,
                    Detail = "Facility is already assigned to the specified building."
                });
            }




            // Validate if user is allowed to make this changes
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if(!userRoles.Contains("Admin"))
            {
                if (userRoles.Contains("Tenant") && (building.TenantId == null || building.TenantId != Guid.Parse(userIdClaim.Value)))
                {
                    _logger.LogWarning("Assign facility to building failed: tenant {UserId} is not authorized for building code {BuildingCode}", userIdClaim.Value, request.BuildingCode);
                    return new AssignFacilityToBuildingResult(null, new ErrorCarrier()
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You are not allowed to make this changes."
                    });
                }

                if (userRoles.Contains("ComplexManager") && (building.Area!.ComplexManagerId == null || building.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value)))
                {
                    _logger.LogWarning("Assign facility to building failed: complex manager {UserId} is not authorized for building code {BuildingCode}", userIdClaim.Value, request.BuildingCode);
                    return new AssignFacilityToBuildingResult(null, new ErrorCarrier()
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You are not allowed to make this changes."
                    });
                }

            }




            // Make changes in database
            try
            {
                int updated = await _areaDbContext.Facilities.Where(f => f.Id == facility.Id).ExecuteUpdateAsync(setters => setters.SetProperty(f => f.BuildingId, building.Id).SetProperty(f => f.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (updated == 0)
                {
                    _logger.LogError("Assign facility to building failed: update returned 0 rows for facility code {FacilityCode} and building code {BuildingCode}", request.FacilityCode, request.BuildingCode);
                    return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign facility to the building."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Assign facility to building failed: database error for facility code {FacilityCode} and building code {BuildingCode}", request.FacilityCode, request.BuildingCode);
                return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                {
                    Title = "DATABASE_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning the facility."
                });
            }

            _logger.LogInformation("Facility code {FacilityCode} assigned successfully to building code {BuildingCode}", request.FacilityCode, request.BuildingCode);
            return new AssignFacilityToBuildingResult(new AssignFacilityToBuildingResponse(true, "Facility assigned to building successfully."), null);
        }
    }
}
