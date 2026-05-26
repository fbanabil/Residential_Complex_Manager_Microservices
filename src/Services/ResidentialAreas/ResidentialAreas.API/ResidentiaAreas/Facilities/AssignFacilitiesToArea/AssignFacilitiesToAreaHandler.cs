using ResidentialAreas.API.Helpers.ErrorCarrier;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AssignFacilitiesToArea
{
    public record AssignFacilitiesToAreaCommand(long AreaCode, List<long> FacilityCodes) : ICommand<AssignFacilitiesToAreaResult>;
    public record AssignFacilitiesToAreaResult(AssignFacilitiesToAreaResponse? Result, ErrorCarrier? Error);

    public class AssignFacilitiesToAreaHandler : ICommandHandler<AssignFacilitiesToAreaCommand, AssignFacilitiesToAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AssignFacilitiesToAreaHandler> _logger;

        public AssignFacilitiesToAreaHandler(AreaDbContext areaDbContext, IHttpContextAccessor httpContextAccessor, ILogger<AssignFacilitiesToAreaHandler> logger)
        {
            _areaDbContext = areaDbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AssignFacilitiesToAreaResult> Handle(AssignFacilitiesToAreaCommand request, CancellationToken cancellationToken)
        {
            // Existing area validation
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Assign facilities to area failed: no area found with code {AreaCode}", request.AreaCode);
                return new AssignFacilitiesToAreaResult(null, new ErrorCarrier
                {
                    Title = "AREA_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No area found with code {request.AreaCode}."
                });
            }



            // Get claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();


            // Validate if not admin then must be the complex manager
            if(!userRoles.Contains("Admin"))
            {
                if(area.ComplexManagerId == null || area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
                {
                    _logger.LogWarning("Assign facilities to area failed: user {UserId} is not authorized for area code {AreaCode}", userIdClaim.Value, request.AreaCode);
                    return new AssignFacilitiesToAreaResult(null, new ErrorCarrier()
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You are not authorized to do this action"
                    });
                }
            }


            // Validate facilities
            List<long> facilityCodes = request.FacilityCodes.Distinct().ToList();
            List<Facility> facilities = await _areaDbContext.Facilities.AsNoTracking().Where(f => f.FacilityCode.HasValue && facilityCodes.Contains(f.FacilityCode.Value)).ToListAsync(cancellationToken);
            if (facilities.Count != facilityCodes.Count)
            {
                List<long> missingCodes = facilityCodes.Except(facilities.Select(f => f.FacilityCode ?? 0)).ToList();
                _logger.LogWarning("Assign facilities to area failed: missing facility codes {MissingCodes} for area code {AreaCode}", string.Join(", ", missingCodes), request.AreaCode);
                return new AssignFacilitiesToAreaResult(null, new ErrorCarrier
                {
                    Title = "FACILITIES_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"Missing facility codes: {string.Join(", ", missingCodes)}."
                });
            }


            // Assign in database
            try
            {
                int updated = await _areaDbContext.Facilities.Where(f => f.FacilityCode.HasValue && facilityCodes.Contains(f.FacilityCode.Value)).ExecuteUpdateAsync(setters => setters.SetProperty(f => f.AreaId, area.Id).SetProperty(f => f.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (updated == 0)
                {
                    _logger.LogError("Assign facilities to area failed: update returned 0 rows for area code {AreaCode}", request.AreaCode);
                    return new AssignFacilitiesToAreaResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign facilities to the area."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Assign facilities to area failed: database error for area code {AreaCode}", request.AreaCode);
                return new AssignFacilitiesToAreaResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning facilities."
                });
            }

            _logger.LogInformation("{Count} facility(s) assigned successfully to area code {AreaCode}", facilityCodes.Count, request.AreaCode);
            return new AssignFacilitiesToAreaResult(new AssignFacilitiesToAreaResponse(true, $"{facilityCodes.Count} facility(s) assigned to area {request.AreaCode}."), null);
        }
    }
}
