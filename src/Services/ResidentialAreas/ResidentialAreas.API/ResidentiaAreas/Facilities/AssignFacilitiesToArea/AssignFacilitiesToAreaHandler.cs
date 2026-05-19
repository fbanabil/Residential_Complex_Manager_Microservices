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

        public AssignFacilitiesToAreaHandler(AreaDbContext areaDbContext, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AssignFacilitiesToAreaResult> Handle(AssignFacilitiesToAreaCommand request, CancellationToken cancellationToken)
        {
            // Existing area validation
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
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
                    return new AssignFacilitiesToAreaResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign facilities to the area."
                    });
                }
            }
            catch
            {
                return new AssignFacilitiesToAreaResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning facilities."
                });
            }

            return new AssignFacilitiesToAreaResult(new AssignFacilitiesToAreaResponse(true, $"{facilityCodes.Count} facility(s) assigned to area {request.AreaCode}."), null);
        }
    }
}