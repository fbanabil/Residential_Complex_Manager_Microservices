using ResidentialAreas.API.Helpers.ErrorCarrier;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.AssignParkingSpaceToArea
{
    public record AssignParkingSpaceToAreaCommand(long AreaCode, long ParkingSpaceCode) : ICommand<AssignParkingSpaceToAreaResult>;
    public record AssignParkingSpaceToAreaResult(AssignParkingSpaceToAreaResponse? Result, ErrorCarrier? Error);

    public class AssignParkingSpaceToAreaHandler : ICommandHandler<AssignParkingSpaceToAreaCommand, AssignParkingSpaceToAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;

        public AssignParkingSpaceToAreaHandler(AreaDbContext areaDbContext)
        {
            _areaDbContext = areaDbContext;
        }

        public async Task<AssignParkingSpaceToAreaResult> Handle(AssignParkingSpaceToAreaCommand request, CancellationToken cancellationToken)
        {
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
                return new AssignParkingSpaceToAreaResult(null, new ErrorCarrier
                {
                    Title = "AREA_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No area found with code {request.AreaCode}."
                });
            }


            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking().FirstOrDefaultAsync(p => p.ParkingSpaceCode == request.ParkingSpaceCode, cancellationToken);
            if (parkingSpace == null)
            {
                return new AssignParkingSpaceToAreaResult(null, new ErrorCarrier
                {
                    Title = "PARKING_SPACE_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No parking space found with code {request.ParkingSpaceCode}."
                });
            }


            if (parkingSpace.AreaId == area.Id)
            {
                return new AssignParkingSpaceToAreaResult(null, new ErrorCarrier
                {
                    Title = "ALREADY_ASSIGNED",
                    StatusCode = 400,
                    Detail = "Parking space is already assigned to the specified area."
                });
            }

            
            
            bool duplicateName = await _areaDbContext.ParkingSpaces.AsNoTracking().AnyAsync(p => p.AreaId == area.Id && p.Name == parkingSpace.Name && p.Id != parkingSpace.Id, cancellationToken);
            if (duplicateName)
            {
                return new AssignParkingSpaceToAreaResult(null, new ErrorCarrier
                {
                    Title = "DUPLICATE_PARKING_SPACE",
                    StatusCode = 400,
                    Detail = $"A parking space named {parkingSpace.Name} already exists in area {request.AreaCode}."
                });
            }

            try
            {
                int updated = await _areaDbContext.ParkingSpaces.Where(p => p.Id == parkingSpace.Id).ExecuteUpdateAsync(setters => setters.SetProperty(p => p.AreaId, area.Id).SetProperty(p => p.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (updated == 0)
                {
                    return new AssignParkingSpaceToAreaResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign parking space to the area."
                    });
                }
            }
            catch
            {
                return new AssignParkingSpaceToAreaResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning the parking space."
                });
            }

            return new AssignParkingSpaceToAreaResult(new AssignParkingSpaceToAreaResponse(true, "Parking space assigned to area successfully."), null);
        }
    }
}