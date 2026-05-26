
using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AddNewBuilding
{

    public record AddNewBuildingCommand(long AreaCode, string Name, string BlockNo, int TotalFloors, string Address, string Status, List<string?>? ImageBase64) : ICommand<AddNewBuildingResult>;

    public record AddNewBuildingResult(AddNewBuildingResponse? Result,ErrorCarrier? Error);

    public class AddNewBuildingHandler : ICommandHandler<AddNewBuildingCommand, AddNewBuildingResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewBuildingHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;
       

        public AddNewBuildingHandler(AreaDbContext areaDbContext, ILogger<AddNewBuildingHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<AddNewBuildingResult> Handle(AddNewBuildingCommand request, CancellationToken cancellationToken)
        {
            // Validate input
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Add new building failed: no area found with code {AreaCode}", request.AreaCode);
                return new AddNewBuildingResult(null, new ErrorCarrier()
                {
                    Title = "NOT FOUND",
                    Detail = $"Area with code {request.AreaCode} not found.",
                    StatusCode = StatusCodes.Status404NotFound
                });
            }



            // Authorization check: Only Admins or the Complex Manager of the area can add buildings
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if(!userRoles.Contains("Admin") && area.ComplexManagerId != null && area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
            {
                _logger.LogWarning("Add new building failed: user {UserId} is not authorized for area code {AreaCode}", userIdClaim.Value, request.AreaCode);
                return new AddNewBuildingResult(null, new ErrorCarrier()
                {
                    Title = "FORBIDDEN",
                    Detail = $"User does not have permission to add building to this area.",
                    StatusCode = StatusCodes.Status403Forbidden
                });
            }





            // Save images first
            List<string?>? imagePaths = new List<string?>();
            try
            {
                imagePaths = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/Buildings");
            }
            catch
            {
                _logger.LogError("Failed to save images for building {BuildingName} in area {AreaCode}", request.Name, request.AreaCode);
                 return new AddNewBuildingResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    Detail = $"Failed to save images for the building.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }


            // Create building entity
            Building building = new Building
            {
                Id = Guid.NewGuid(),
                AreaId= area!.Id,
                Name = request.Name,
                BlockNo = request.BlockNo,
                TotalFloors = request.TotalFloors,
                Address = request.Address,
                Status = System.Enum.Parse<Status>(request.Status),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };


            // Start transaction
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);


            // Save building
            try
            {
                await _areaDbContext.Buildings.AddAsync(building, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _logger.LogError("Failed to add building {BuildingName} to area {AreaCode}", request.Name, request.AreaCode);
                await transaction.RollbackAsync(cancellationToken);
                return new AddNewBuildingResult(null, new ErrorCarrier()
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    Detail = $"Failed to add the building to the database.",
                    StatusCode = StatusCodes.Status500InternalServerError
                });
            }



            // Create image records if there are images
            if (imagePaths != null && imagePaths.Count > 0)
            {
                List<Image> imgs = imagePaths.Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    Url = path,
                    ImageType = ImageType.Building,
                    BuildingCode = building.Code,
                }).ToList();



                // Add image records to database
                try
                {
                    await _areaDbContext.Images.AddRangeAsync(imgs);
                    await _areaDbContext.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    _logger.LogError("Failed to save image records for building {BuildingName} in area {AreaCode}", request.Name, request.AreaCode);
                    await transaction.RollbackAsync(cancellationToken);
                    return new AddNewBuildingResult(null, new ErrorCarrier()
                    {
                        Title = "INTERNAL_SERVER_ERROR",
                        Detail = $"Failed to save image records for the building.",
                        StatusCode = StatusCodes.Status500InternalServerError
                    });
                }
            }


            // Commit transaction
            await transaction.CommitAsync(cancellationToken);


            // Map to response
            _logger.LogInformation("Building '{BuildingName}' created successfully with code {BuildingCode} in area code {AreaCode}", building.Name, building.Code, area.Code);
            return new AddNewBuildingResult(new AddNewBuildingResponse(building.Id.Value, building.Code, building.Name!, area.Name, area.Code), null);
        }
    }
}
