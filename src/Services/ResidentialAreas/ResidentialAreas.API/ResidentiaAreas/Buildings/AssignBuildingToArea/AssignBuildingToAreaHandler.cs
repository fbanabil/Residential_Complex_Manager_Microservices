using ResidentialAreas.API.Helpers.ErrorCarrier;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AssignBuildingToArea
{
    public record AssignBuildingToAreaCommand(long AreaCode, List<long> BuildingCodes) : ICommand<AssignBuildingToAreaResult>;
    public record AssignBuildingToAreaResult(AssignBuildingToAreaResponse? Result, ErrorCarrier? Error);

    public class AssignBuildingToAreaHandler : ICommandHandler<AssignBuildingToAreaCommand, AssignBuildingToAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AssignBuildingToAreaHandler> _logger;

        public AssignBuildingToAreaHandler(AreaDbContext areaDbContext, IHttpContextAccessor httpContextAccessor, ILogger<AssignBuildingToAreaHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AssignBuildingToAreaResult> Handle(AssignBuildingToAreaCommand request, CancellationToken cancellationToken)
        {
            // Validate area existence and permissions
            Area? area = await _areaDbContext.Areas.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Assign building to area failed: no area found with code {AreaCode}", request.AreaCode);
                return new AssignBuildingToAreaResult(null, new ErrorCarrier
                {
                    Title = "AREA_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No area found with code {request.AreaCode}."
                });
            }



            // Validate user permissions
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();


            // Only allow Admins or the Complex Manager of the area to assign buildings
            if ((!userRoles.Contains("Admin")) && userIdClaim != null && userIdClaim.Value != area.ComplexManagerId.ToString())
            {
                _logger.LogWarning("Assign building to area failed: user {UserId} is not authorized for area code {AreaCode}", userIdClaim.Value, request.AreaCode);
                return new AssignBuildingToAreaResult(null, new ErrorCarrier
                {
                    Title = "UNAUTHORIZED",
                    StatusCode = 403,
                    Detail = "You are not authorized to assign buildings to this area."
                });
            }



            // Validate building codes by fetching them from the database and comparing counts
            List<long> buildingCodes = request.BuildingCodes.Distinct().ToList();
            List<Building> buildings = await _areaDbContext.Buildings.AsNoTracking().Where(b => buildingCodes.Contains(b.Code)).ToListAsync(cancellationToken);
            if (buildings.Count != buildingCodes.Count)
            {
                List<long> missingCodes = buildingCodes.Except(buildings.Select(b => b.Code)).ToList();
                _logger.LogWarning("Assign building to area failed: missing building codes {MissingCodes} for area code {AreaCode}", string.Join(", ", missingCodes), request.AreaCode);
                return new AssignBuildingToAreaResult(null, new ErrorCarrier
                {
                    Title = "BUILDINGS_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"Missing building codes: {string.Join(", ", missingCodes)}."
                });
            }



            // Check if any building is already assigned to a different area
            int conflicts = buildings.Count(b => b.AreaId != null && b.AreaId != area.Id);
            if(conflicts > 0)
            {
                _logger.LogWarning("Assign building to area failed: {ConflictCount} building(s) already assigned to a different area (area code {AreaCode})", conflicts, request.AreaCode);
                return new AssignBuildingToAreaResult(null, new ErrorCarrier
                {
                    Title = "BUILDING_ASSIGNMENT_CONFLICT",
                    StatusCode = 409,
                    Detail = $"{conflicts} building(s) are already assigned to a different area."
                });
            }




            // Perform bulk update to assign buildings to the area
            try
            {
                int updated = await _areaDbContext.Buildings
                    .Where(b => buildingCodes.Contains(b.Code))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(b => b.AreaId, area.Id)
                        .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (updated == 0)
                {
                    _logger.LogError("Assign building to area failed: update returned 0 rows for area code {AreaCode}", request.AreaCode);
                    return new AssignBuildingToAreaResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign buildings to the area."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Assign building to area failed: database error for area code {AreaCode}", request.AreaCode);
                return new AssignBuildingToAreaResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning buildings."
                });
            }


            _logger.LogInformation("{Count} building(s) assigned successfully to area code {AreaCode}", buildingCodes.Count, request.AreaCode);
            return new AssignBuildingToAreaResult(new AssignBuildingToAreaResponse(true, $"{buildingCodes.Count} building(s) assigned to area {request.AreaCode}."), null);
        }
    }
}
