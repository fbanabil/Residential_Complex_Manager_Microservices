using ResidentialAreas.API.Helpers.ErrorCarrier;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AssignFacilitiesToArea
{
    public record AssignFacilitiesToAreaCommand(long AreaCode, List<long> FacilityCodes) : ICommand<AssignFacilitiesToAreaResult>;
    public record AssignFacilitiesToAreaResult(AssignFacilitiesToAreaResponse? Result, ErrorCarrier? Error);

    public class AssignFacilitiesToAreaHandler : ICommandHandler<AssignFacilitiesToAreaCommand, AssignFacilitiesToAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;

        public AssignFacilitiesToAreaHandler(AreaDbContext areaDbContext)
        {
            _areaDbContext = areaDbContext;
        }

        public async Task<AssignFacilitiesToAreaResult> Handle(AssignFacilitiesToAreaCommand request, CancellationToken cancellationToken)
        {
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