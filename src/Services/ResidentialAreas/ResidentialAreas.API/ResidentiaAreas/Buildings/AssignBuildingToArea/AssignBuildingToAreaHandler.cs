using ResidentialAreas.API.Helpers.ErrorCarrier;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AssignBuildingToArea
{
    public record AssignBuildingToAreaCommand(long AreaCode, List<long> BuildingCodes) : ICommand<AssignBuildingToAreaResult>;
    public record AssignBuildingToAreaResult(AssignBuildingToAreaResponse? Result, ErrorCarrier? Error);

    public class AssignBuildingToAreaHandler : ICommandHandler<AssignBuildingToAreaCommand, AssignBuildingToAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;

        public AssignBuildingToAreaHandler(AreaDbContext areaDbContext)
        {
            _areaDbContext = areaDbContext;
        }

        public async Task<AssignBuildingToAreaResult> Handle(AssignBuildingToAreaCommand request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);

            if (area == null)
            {
                return new AssignBuildingToAreaResult(null, new ErrorCarrier
                {
                    Title = "AREA_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No area found with code {request.AreaCode}."
                });
            }

            List<long> buildingCodes = request.BuildingCodes.Distinct().ToList();
            List<Building> buildings = await _areaDbContext.Buildings.AsNoTracking()
                .Where(b => buildingCodes.Contains(b.Code))
                .ToListAsync(cancellationToken);

            if (buildings.Count != buildingCodes.Count)
            {
                List<long> missingCodes = buildingCodes.Except(buildings.Select(b => b.Code)).ToList();
                return new AssignBuildingToAreaResult(null, new ErrorCarrier
                {
                    Title = "BUILDINGS_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"Missing building codes: {string.Join(", ", missingCodes)}."
                });
            }

            try
            {
                int updated = await _areaDbContext.Buildings
                    .Where(b => buildingCodes.Contains(b.Code))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(b => b.AreaId, area.Id)
                        .SetProperty(b => b.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (updated == 0)
                {
                    return new AssignBuildingToAreaResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign buildings to the area."
                    });
                }
            }
            catch
            {
                return new AssignBuildingToAreaResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning buildings."
                });
            }

            return new AssignBuildingToAreaResult(new AssignBuildingToAreaResponse(true, $"{buildingCodes.Count} building(s) assigned to area {request.AreaCode}."), null);
        }
    }
}