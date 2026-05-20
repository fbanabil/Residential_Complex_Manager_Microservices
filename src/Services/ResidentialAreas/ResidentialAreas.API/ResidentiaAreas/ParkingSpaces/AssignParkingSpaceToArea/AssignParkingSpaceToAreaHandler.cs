using ResidentialAreas.API.Helpers.ErrorCarrier;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.AssignParkingSpaceToArea
{
    public record AssignParkingSpaceToAreaCommand(long AreaCode, long ParkingSpaceCode) : ICommand<AssignParkingSpaceToAreaResult>;
    public record AssignParkingSpaceToAreaResult(AssignParkingSpaceToAreaResponse? Result, ErrorCarrier? Error);

    public class AssignParkingSpaceToAreaHandler : ICommandHandler<AssignParkingSpaceToAreaCommand, AssignParkingSpaceToAreaResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AssignParkingSpaceToAreaHandler(AreaDbContext areaDbContext, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AssignParkingSpaceToAreaResult> Handle(AssignParkingSpaceToAreaCommand request, CancellationToken cancellationToken)
        {
            // Validate area code
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




            // Validate parking space code
            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking().Include(ps => ps.Area).FirstOrDefaultAsync(p => p.ParkingSpaceCode == request.ParkingSpaceCode, cancellationToken);
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



            // Validate if the user have the permission to assign parking spaces to the area (must be the complex manager of the area or an admin)
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if(!userRoles.Contains("Admin"))
            {
                if(area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
                {
                    return new AssignParkingSpaceToAreaResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to assign parking spaces to this area."
                    });
                }

                if(parkingSpace.Area!=null && parkingSpace.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
                {
                    return new AssignParkingSpaceToAreaResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to reassign this parking space from its current area."
                    });
                }
            }




            // Validate duplicate parking space name in the same area
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



            // Assign parking space to area
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