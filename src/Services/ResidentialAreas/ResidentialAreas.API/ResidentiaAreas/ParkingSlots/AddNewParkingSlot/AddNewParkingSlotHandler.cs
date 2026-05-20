using ResidentialAreas.API.Helpers.ErrorCarrier;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSlots.AddNewParkingSlot
{
    public record AddNewParkingSlotCommand(long ParkingSpaceCode, long? AssignedUnitCode,string SlotType,string Status) : ICommand<AddNewParkingSlotResult>;

    public record AddNewParkingSlotResult(AddNewParkingSlotResponse? Result, ErrorCarrier? Error);

    public class AddNewParkingSlotHandler : ICommandHandler<AddNewParkingSlotCommand, AddNewParkingSlotResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewParkingSlotHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AddNewParkingSlotHandler(AreaDbContext areaDbContext, ILogger<AddNewParkingSlotHandler> logger, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AddNewParkingSlotResult> Handle(AddNewParkingSlotCommand request, CancellationToken cancellationToken)
        {
            // Validate parking space existence
            ParkingSpace? parkingSpace = await _areaDbContext.ParkingSpaces.AsNoTracking().Include(ps => ps.Area)
                .FirstOrDefaultAsync(p => p.ParkingSpaceCode == request.ParkingSpaceCode, cancellationToken);
            if (parkingSpace == null)
            {
                _logger.LogWarning("Parking space with code {ParkingSpaceCode} not found while creating parking slot.", request.ParkingSpaceCode);
                return new AddNewParkingSlotResult(null, new ErrorCarrier
                {
                    Title = "PARKING_SPACE_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"Parking space with code {request.ParkingSpaceCode} not found."
                });
            }

            


            // Validate assigned unit existence if provided
            EntityModels.Unit? unit = null;
            if (request.AssignedUnitCode.HasValue)
            {
                unit = await _areaDbContext.Units.AsNoTracking().Include(u => u.Building)
                    .FirstOrDefaultAsync(u => u.Code == request.AssignedUnitCode.Value, cancellationToken);
                if (unit == null)
                {
                    _logger.LogWarning("Assigned unit with code {UnitCode} not found while creating parking slot.", request.AssignedUnitCode.Value);
                    return new AddNewParkingSlotResult(null, new ErrorCarrier
                    {
                        Title = "ASSIGNED_UNIT_NOT_FOUND",
                        StatusCode = 404,
                        Detail = $"Assigned unit with code {request.AssignedUnitCode.Value} not found."
                    });
                }

                if (unit.Building?.AreaId != null && unit.Building?.AreaId != parkingSpace.AreaId)
                {
                    _logger.LogWarning("Assigned unit with code {UnitCode} does not belong to the same area as the parking space while creating parkingslot.", request.AssignedUnitCode.Value);
                    return new AddNewParkingSlotResult(null, new ErrorCarrier
                    {
                        Title = "UNIT_AREA_MISMATCH",
                        StatusCode = 400,
                        Detail = $"Assigned unit with code {request.AssignedUnitCode.Value} does not belong to the same area as the parking space."
                    });
                }
            }




            // Validate if user has permission to create parking slot in this area
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if(!userRoles.Contains("Admin"))
            {
                if(parkingSpace.Area?.ComplexManagerId == null || parkingSpace.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
                {
                    _logger.LogWarning("User with ID {UserId} does not have permission to create parking slot in this area.", userIdClaim.Value);
                    return new AddNewParkingSlotResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = 403,
                        Detail = "You do not have permission to create a parking slot in this area."
                    });
                }
            }


            // Create new parking slot
            ParkingSlot parkingSlot = new ParkingSlot
            {
                Id = Guid.NewGuid(),
                ParkingSpaceId = parkingSpace.Id,
                AssignedUnitId = unit?.Id,
                SlotType = System.Enum.Parse<SlotType>(request.SlotType, true),
                Status = System.Enum.Parse<Status>(request.Status, true),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };



            // Begin transaction to ensure atomicity of operations
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);



            // Add parking slot to database
            try
            {
                await _areaDbContext.ParkingSlots.AddAsync(parkingSlot, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("An error occurred while creating a new parking slot for parking space code {ParkingSpaceCode}.", request.ParkingSpaceCode);
                return new AddNewParkingSlotResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while creating the parking slot. Please try again later."
                });
            }



            // Commit transaction
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("An error occurred while committing the transaction for creating a new parking slot for parking space code {ParkingSpaceCode}.", request.ParkingSpaceCode);
                await transaction.RollbackAsync(cancellationToken);
                return new AddNewParkingSlotResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while finalizing the creation of the parking slot. Please try again later."
                });
            }



            return new AddNewParkingSlotResult(new AddNewParkingSlotResponse
                (
                parkingSlot.Id ?? Guid.Empty,
                parkingSlot.SlotCode,
                parkingSlot.SlotType.ToString(),
                parkingSlot.Status.ToString(),
                parkingSpace.ParkingSpaceCode,
                parkingSpace.Name ?? string.Empty,
                unit?.Code,
                unit?.UnitNo), null);
        }
    }
}
