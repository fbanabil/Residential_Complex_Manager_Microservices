using ResidentialAreas.API.Helpers.ErrorCarrier;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AssignFacilityToBuilding
{
    public record AssignFacilityToBuildingCommand(long FacilityCode, long BuildingCode) : ICommand<AssignFacilityToBuildingResult>;
    public record AssignFacilityToBuildingResult(AssignFacilityToBuildingResponse? Result, ErrorCarrier? Error);

    public class AssignFacilityToBuildingHandler : ICommandHandler<AssignFacilityToBuildingCommand, AssignFacilityToBuildingResult>
    {
        private readonly AreaDbContext _areaDbContext;

        public AssignFacilityToBuildingHandler(AreaDbContext areaDbContext)
        {
            _areaDbContext = areaDbContext;
        }

        public async Task<AssignFacilityToBuildingResult> Handle(AssignFacilityToBuildingCommand request, CancellationToken cancellationToken)
        {
            Building? building = await _areaDbContext.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Code == request.BuildingCode, cancellationToken);
            if (building == null)
            {
                return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                {
                    Title = "BUILDING_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No building found with code {request.BuildingCode}."
                });
            }



            Facility? facility = await _areaDbContext.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.FacilityCode == request.FacilityCode, cancellationToken);
            if (facility == null)
            {
                return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                {
                    Title = "FACILITY_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No facility found with code {request.FacilityCode}."
                });
            }



            if (facility.BuildingId == building.Id)
            {
                return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                {
                    Title = "ALREADY_ASSIGNED",
                    StatusCode = 400,
                    Detail = "Facility is already assigned to the specified building."
                });
            }



            try
            {
                int updated = await _areaDbContext.Facilities.Where(f => f.Id == facility.Id).ExecuteUpdateAsync(setters => setters.SetProperty(f => f.BuildingId, building.Id).SetProperty(f => f.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (updated == 0)
                {
                    return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign facility to the building."
                    });
                }
            }
            catch
            {
                return new AssignFacilityToBuildingResult(null, new ErrorCarrier
                {
                    Title = "DATABASE_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning the facility."
                });
            }


            return new AssignFacilityToBuildingResult(new AssignFacilityToBuildingResponse(true, "Facility assigned to building successfully."), null);
        }
    }
}