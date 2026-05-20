using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Units.AddNewUnit
{
    public record AddNewUnitCommand(long BuildingCode, string UnitNo, int FloorNo, string UnitType, int? Bedrooms, int? Bathrooms, decimal AreaSqft, string OccupancyStatus, string OwnershipType, Guid? CurrentLeaseId, List<string?>? ImageBase64) : ICommand<AddNewUnitResult>;

    public record AddNewUnitResult(AddNewUnitResponse? Result, ErrorCarrier? Error);

    public class AddNewUnitHandler : ICommandHandler<AddNewUnitCommand, AddNewUnitResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewUnitHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AddNewUnitHandler(AreaDbContext areaDbContext, ILogger<AddNewUnitHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AddNewUnitResult> Handle(AddNewUnitCommand request, CancellationToken cancellationToken)
        {
            // Validate building existence
            Building? building = await _areaDbContext.Buildings.AsNoTracking().Include(b => b.Area)
                .FirstOrDefaultAsync(b => b.Code == request.BuildingCode, cancellationToken);
            if (building == null)
            {
                _logger.LogWarning("Building with code {BuildingCode} not found while creating unit.", request.BuildingCode);
                return new AddNewUnitResult(null, new ErrorCarrier
                {
                    Title = "BUILDING_NOT_FOUND",
                    StatusCode = StatusCodes.Status404NotFound,
                    Detail = $"No building found with code {request.BuildingCode}."
                });
            }




            // Validate unit number uniqueness within the building
            bool unitExists = await _areaDbContext.Units.AsNoTracking()
                .AnyAsync(u => u.BuildingId == building.Id && u.UnitNo == request.UnitNo, cancellationToken);
            if (unitExists)
            {
                _logger.LogWarning("Unit number {UnitNo} already exists in building {BuildingCode}.", request.UnitNo, request.BuildingCode);
                return new AddNewUnitResult(null, new ErrorCarrier
                {
                    Title = "UNIT_ALREADY_EXISTS",
                    StatusCode = StatusCodes.Status409Conflict,
                    Detail = $"A unit with number {request.UnitNo} already exists in building with code {request.BuildingCode}."
                });
            }




            // Authorization check: Only Admins, Complex Managers, or Tenants associated with the building can create units
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if (!userRoles.Contains("Admin"))
            {
                bool isComplexManager = userRoles.Contains("ComplexManager");
                bool isTenant = userRoles.Contains("Tenant");


                if (isComplexManager && (building.Area == null || building.Area?.ComplexManagerId == null || building.Area.ComplexManagerId != Guid.Parse(userIdClaim.Value)))
                {
                    _logger.LogWarning("Complex manager with user ID {UserId} attempted to create a unit in building {BuildingCode} that they do not manage.", userIdClaim.Value, request.BuildingCode);
                    return new AddNewUnitResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You do not have permission to create a unit in this building."
                    });
                }

                if (isTenant && (building.TenantId == null || building.TenantId != Guid.Parse(userIdClaim.Value)))
                {
                    _logger.LogWarning("Tenant with user ID {UserId} attempted to create a unit in building {BuildingCode} that they do not reside in.", userIdClaim.Value, request.BuildingCode);
                    return new AddNewUnitResult(null, new ErrorCarrier
                    {
                        Title = "FORBIDDEN",
                        StatusCode = StatusCodes.Status403Forbidden,
                        Detail = "You do not have permission to create a unit in this building."
                    });
                }
            }




            // Save images and get their paths
            List<string?>? imagePaths = new List<string?>();
            try
            {
                imagePaths = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/Units");
            }
            catch
            {
                _logger.LogError("An error occurred while saving images for the new unit in building {BuildingCode}.", request.BuildingCode);
                return new AddNewUnitResult(null, new ErrorCarrier
                {
                    Title = "IMAGE_SAVE_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while saving the unit images. Please try again later."
                });
            }



            // Create the new unit entity
            EntityModels.Unit unit = new EntityModels.Unit
            {
                Id = Guid.NewGuid(),
                BuildingId = building.Id!.Value,
                UnitNo = request.UnitNo,
                FloorNo = request.FloorNo,
                UnitType = System.Enum.Parse<UnitType>(request.UnitType, true),
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                AreaSqft = request.AreaSqft,
                OccupancyStatus = System.Enum.Parse<OccupancyStatus>(request.OccupancyStatus, true),
                OwnershipType = System.Enum.Parse<OwnershipType>(request.OwnershipType, true),
                CurrentLeaseId = request.CurrentLeaseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };



            // Use a transaction to ensure atomicity of unit and image creation
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);



            // Save the new unit to the database
            try
            {
                await _areaDbContext.Units.AddAsync(unit, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("An error occurred while saving the new unit in building {BuildingCode}.", request.BuildingCode);
                return new AddNewUnitResult(null, new ErrorCarrier
                {
                    Title = "UNIT_SAVE_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while saving the unit. Please try again later."
                });
            }



            // If there are images, save them to the database
            if (imagePaths != null && imagePaths.Count > 0)
            {
                List<Image> images = imagePaths.Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    Url = path,
                    ImageType = ImageType.Unit,
                    UnitCode = unit.Code
                }).ToList();



                // Save the images to the database
                try
                {
                    await _areaDbContext.Images.AddRangeAsync(images, cancellationToken);
                    await _areaDbContext.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("An error occurred while saving images for the new unit in building {BuildingCode}.", request.BuildingCode);
                    return new AddNewUnitResult(null, new ErrorCarrier
                    {
                        Title = "IMAGE_SAVE_ERROR",
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Detail = "An error occurred while saving the unit images. Please try again later."
                    });
                }
            }


            // Commit the transaction after both unit and images are saved successfully
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("An error occurred while committing the transaction for the new unit in building {BuildingCode}.", request.BuildingCode);
                await transaction.RollbackAsync(cancellationToken);
                return new AddNewUnitResult(null, new ErrorCarrier
                {
                    Title = "TRANSACTION_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while finalizing the unit creation. Please try again later."
                });
            }

            var httpContext = _httpContextAccessor.HttpContext;
            imagePaths = imagePaths?.Select(path => $"{httpContext?.Request.Scheme}://{httpContext?.Request.Host}/{path}").ToList()!;

            return new AddNewUnitResult(new AddNewUnitResponse(
            unit.Id,
            unit.Code,
            unit.UnitNo,
            unit.FloorNo,
            unit.UnitType.ToString(),
            unit.Bedrooms,
            unit.Bathrooms,
            unit.AreaSqft,
            unit.OccupancyStatus.ToString(),
            unit.OwnershipType.ToString(),
            unit.CurrentLeaseId,
            building.Code,
            building.Name ?? string.Empty,
            imagePaths), null);
        }
    }
}
