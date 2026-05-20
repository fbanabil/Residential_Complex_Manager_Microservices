using ResidentialAreas.API.Helpers.ErrorCarrier;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.AssigtnParkingSlotsToParkingSpace
{
    public record AssigtnParkingSlotsToParkingSpaceCommand(long ParkingSpaceCode, List<long> SlotCodes) : ICommand<AssigtnParkingSlotsToParkingSpaceResult>;
    public record AssigtnParkingSlotsToParkingSpaceResult(AssigtnParkingSlotsToParkingSpaceResponse? Result, ErrorCarrier? Error);

    public class AssigtnParkingSlotsToParkingSpaceHandler : ICommandHandler<AssigtnParkingSlotsToParkingSpaceCommand, AssigtnParkingSlotsToParkingSpaceResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AssigtnParkingSlotsToParkingSpaceHandler(AreaDbContext areaDbContext, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AssigtnParkingSlotsToParkingSpaceResult> Handle(AssigtnParkingSlotsToParkingSpaceCommand request, CancellationToken cancellationToken)
        {
            // Validate parking space existence
            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking().Include(ps => ps.Area).FirstOrDefaultAsync(p => p.ParkingSpaceCode == request.ParkingSpaceCode, cancellationToken);
            if (parkingSpace == null)
            {
                return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "PARKING_SPACE_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No parking space found with code {request.ParkingSpaceCode}."
                });
            }



            // Authorization check: Only Admins or the Complex Manager of the area can assign parking slots
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();



            // If the user is not an Admin, check if they are the Complex Manager of the area
            if (!userRoles.Contains("Admin"))
            {
                if (parkingSpace.Area?.ComplexManagerId == null || parkingSpace.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
                {
                    return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to assign parking slots to this parking space."
                    });
                }
            }



            // Validate parking slot existence
            List<long> slotCodes = request.SlotCodes.Distinct().ToList();
            List<ParkingSlot> slots = await _areaDbContext.ParkingSlots.AsNoTracking().Where(s => slotCodes.Contains(s.SlotCode)).ToListAsync(cancellationToken);
            if (slots.Count != slotCodes.Count)
            {
                List<long> missingCodes = slotCodes.Except(slots.Select(s => s.SlotCode)).ToList();
                return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "PARKING_SLOTS_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"Missing parking slot codes: {string.Join(", ", missingCodes)}."
                });
            }



            // Perform the update using ExecuteUpdateAsync for efficiency
            try
            {
                int updated = await _areaDbContext.ParkingSlots.Where(s => slotCodes.Contains(s.SlotCode)).ExecuteUpdateAsync(setters => setters.SetProperty(s => s.ParkingSpaceId, parkingSpace.Id).SetProperty(s => s.UpdatedAt, DateTime.UtcNow), cancellationToken);

                if (updated == 0)
                {
                    return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                    {
                        Title = "UPDATE_FAILED",
                        StatusCode = 500,
                        Detail = "Failed to assign parking slots to the parking space."
                    });
                }
            }
            catch
            {
                return new AssigtnParkingSlotsToParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "DATABASE_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while assigning parking slots."
                });
            }



            return new AssigtnParkingSlotsToParkingSpaceResult(new AssigtnParkingSlotsToParkingSpaceResponse(true, $"{slotCodes.Count} parking slot(s) assigned to parking space {request.ParkingSpaceCode}."), null);
        }
    }
}