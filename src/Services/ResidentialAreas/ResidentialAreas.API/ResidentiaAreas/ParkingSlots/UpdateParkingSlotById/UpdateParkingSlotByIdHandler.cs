using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.ResidentiaAreas.ParkingSlots.AssigtnParkingSlotsToParkingSpace;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.UpdateParkingSlotById
{
    public record UpdateParkingSlotByIdCommand(Guid Id,long ParkingSpaceCode, long? AssignedUnitCode, string SlotType,string Status) : ICommand<UpdateParkingSlotByIdResult>;

    public record UpdateParkingSlotByIdResult(UpdateParkingSlotByIdResponse? Result, ErrorCarrier? Error);

    public class UpdateParkingSlotByIdHandler : ICommandHandler<UpdateParkingSlotByIdCommand, UpdateParkingSlotByIdResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<UpdateParkingSlotByIdHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateParkingSlotByIdHandler(AreaDbContext areaDbContext, ILogger<UpdateParkingSlotByIdHandler> logger, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UpdateParkingSlotByIdResult> Handle(UpdateParkingSlotByIdCommand request, CancellationToken cancellationToken)
        {
            ParkingSlot? parkingSlot = await _areaDbContext.ParkingSlots.FirstOrDefaultAsync(ps => ps.Id == request.Id, cancellationToken);
            if (parkingSlot == null)
            {
                _logger.LogWarning("Parking slot with Id {Id} not found for update.", request.Id);
                return new UpdateParkingSlotByIdResult(null, new ErrorCarrier
                {
                    Title = "PARKING_SLOT_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"Parking slot with Id {request.Id} not found."
                });
            }


            
            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ParkingSpaceCode == request.ParkingSpaceCode, cancellationToken);

            if (parkingSpace == null)
            {
                _logger.LogWarning("Parking space with code {ParkingSpaceCode} not found for parking slot update.", request.ParkingSpaceCode);
                return new UpdateParkingSlotByIdResult(null, new ErrorCarrier
                {
                    Title = "PARKING_SPACE_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"Parking space with code {request.ParkingSpaceCode} not found."
                });
            }

            EntityModels.Unit? assignedUnit = null;
            if (request.AssignedUnitCode.HasValue)
            {
                assignedUnit = await _areaDbContext.Units.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Code == request.AssignedUnitCode.Value, cancellationToken);

                if (assignedUnit == null)
                {
                    _logger.LogWarning("Assigned unit with code {UnitCode} not found for parking slot update.", request.AssignedUnitCode.Value);
                    return new UpdateParkingSlotByIdResult(null, new ErrorCarrier
                    {
                        Title = "UNIT_NOT_FOUND",
                        StatusCode = 404,
                        Detail = $"Assigned unit with code {request.AssignedUnitCode.Value} not found."
                    });
                }
            }



            // Authorization check: Only Admins or the Complex Manager of the area can assign parking slots
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();



            // If the user is not an Admin, check if they are the Complex Manager of the area
            if (!userRoles.Contains("Admin"))
            {
                if (parkingSpace.Area?.ComplexManagerId == null || parkingSpace.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
                {
                    return new UpdateParkingSlotByIdResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to assign parking slots to this parking space."
                    });
                }
            }


            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _areaDbContext.ParkingSlots.Where(ps => ps.Id == request.Id)
                    .ExecuteUpdateAsync(ps => ps
                        .SetProperty(p => p.ParkingSpaceId, parkingSpace.Id)
                        .SetProperty(p => p.AssignedUnitId, assignedUnit != null ? assignedUnit.Id : (Guid?)null)
                        .SetProperty(p => p.SlotType, System.Enum.Parse<SlotType>(request.SlotType, true))
                        .SetProperty(p => p.Status, System.Enum.Parse<Status>(request.Status, true))
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                return new UpdateParkingSlotByIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the parking slot. Please try again later."
                });
            }



            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                return new UpdateParkingSlotByIdResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while committing the transaction. Please try again later."
                });
            }



            return new UpdateParkingSlotByIdResult( new UpdateParkingSlotByIdResponse(
                parkingSlot.Id ?? Guid.Empty,
                parkingSlot.SlotCode,
                parkingSlot.SlotType.ToString(),
                parkingSlot.Status.ToString(),
                parkingSlot.CreatedAt ?? DateTime.MinValue,
                parkingSlot.UpdatedAt ?? DateTime.MinValue,
                parkingSpace.ParkingSpaceCode,
                parkingSpace.Name ?? string.Empty,
                assignedUnit?.Code,
                assignedUnit?.UnitNo), null);
        }
    }
}
