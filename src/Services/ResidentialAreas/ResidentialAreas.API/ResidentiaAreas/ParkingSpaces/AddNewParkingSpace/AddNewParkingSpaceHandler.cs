using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.ParkingSpaces.AddNewParkingSpace
{
    public record AddNewParkingSpaceCommand(long AreaCode, string Name, string? Description, string BlockNo, string Status, List<string?>? ImageBase64) : ICommand<AddNewParkingSpaceResult>;
    public record AddNewParkingSpaceResult(AddNewParkingSpaceResponse? Result, ErrorCarrier? ErrorCarrier);

    public class AddNewParkingSpaceHandler : ICommandHandler<AddNewParkingSpaceCommand, AddNewParkingSpaceResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewParkingSpaceHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AddNewParkingSpaceHandler(AreaDbContext areaDbContext, ILogger<AddNewParkingSpaceHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AddNewParkingSpaceResult> Handle(AddNewParkingSpaceCommand request, CancellationToken cancellationToken)
        {
            // Check if area exists
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Area with code {AreaCode} not found while creating parking space.", request.AreaCode);
                return new AddNewParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "Area not found",
                    StatusCode = 404,
                    Detail = $"Area with code {request.AreaCode} not found."
                });
            }


            // Check for duplicate parking space name within the same area
            bool existingParkingSpace = await _areaDbContext.ParkingSpaces.AnyAsync(ps => ps.AreaId == area.Id && ps.Name == request.Name, cancellationToken);
            if (existingParkingSpace) {
                _logger.LogWarning("Parking space with name {ParkingSpaceName} already exists in area {AreaCode}.", request.Name, request.AreaCode);
                return new AddNewParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "Duplicate parking space",
                    StatusCode = 400,
                    Detail = $"Parking space with name {request.Name} already exists in area {request.AreaCode}."
                });
            }



            // Authorization check: Only complex manager of the area or users with "Admin" role can create parking space
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();
            if(!userRoles.Contains("Admin"))
            {
                if (area.ComplexManagerId == null || area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
                {
                    _logger.LogWarning("Unauthorized attempt to create parking space in area {AreaCode} by user {UserId}.", request.AreaCode, userIdClaim.Value);
                    return new AddNewParkingSpaceResult(null, new ErrorCarrier
                    {
                        Title = "Unauthorized",
                        StatusCode = 403,
                        Detail = "You do not have permission to create a parking space in this area."
                    });
                }
            }



            // Save images to file system and get paths
            List<string?>? imagePaths = new List<string?>();
            try
            {
                imagePaths = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/ParkingSpaces");
            }
            catch
            {
                _logger.LogError("Error occurred while saving parking space images to the file system.");
                return new AddNewParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "Image upload error",
                    StatusCode = 500,
                    Detail = "An error occurred while uploading the parking space images. Please upload image later."
                });
            }


            // Create new parking space entity
            ResidentialAreas.API.EntityModels.ParkingSpace parkingSpace = new ResidentialAreas.API.EntityModels.ParkingSpace
            {
                Id = Guid.NewGuid(),
                AreaId = area.Id,
                Name = request.Name,
                Description = request.Description,
                BlockNo = request.BlockNo,
                Status = System.Enum.Parse<Status>(request.Status, true),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };


            // Start a database transaction to ensure data integrity
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);


            // Get the parking space code for the area
            try
            {
                await _areaDbContext.ParkingSpaces.AddAsync(parkingSpace, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while adding new parking space to the database.");
                return new AddNewParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "Database error",
                    StatusCode = 500,
                    Detail = "An error occurred while saving the parking space. Please try again later."
                });
            }


            // Add images to database if there are any
            if (imagePaths != null && imagePaths.Count > 0)
            {
                List<Image> images = imagePaths.Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    Url = path,
                    ImageType = ImageType.ParkingSpace,
                    ParkingSpaceCode = parkingSpace.ParkingSpaceCode
                }).ToList();

                try
                {
                    await _areaDbContext.Images.AddRangeAsync(images, cancellationToken);
                    await _areaDbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "Error occurred while saving parking space images to the database.");
                    return new AddNewParkingSpaceResult(null, new ErrorCarrier
                    {
                        Title = "Image upload error",
                        StatusCode = 500,
                        Detail = "An error occurred while saving the parking space images. Please upload image later."
                    });
                }

            }




            // Commit the transaction after all operations are successful
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while committing the transaction for adding new parking space.");
                return new AddNewParkingSpaceResult(null, new ErrorCarrier
                {
                    Title = "Database error",
                    StatusCode = 500,
                    Detail = "An error occurred while saving the parking space. Please try again later."
                });
            }



            // Convert image paths to URLs
            var httpContext = _httpContextAccessor.HttpContext;
            imagePaths = imagePaths.Select(path => $"{httpContext!.Request.Scheme}://{httpContext!.Request.Host.Value}/{path}").ToList();



            _logger.LogInformation("Parking space '{Name}' created successfully with code {ParkingSpaceCode} in area code {AreaCode}", parkingSpace.Name, parkingSpace.ParkingSpaceCode, area.Code);
            return new AddNewParkingSpaceResult(new AddNewParkingSpaceResponse
                (
                parkingSpace.Id,
                parkingSpace.ParkingSpaceCode,
                parkingSpace.Name ?? string.Empty,
                parkingSpace.Description,
                parkingSpace.BlockNo ?? string.Empty,
                parkingSpace.Status.ToString(),
                area.Code,
                area.Name,
                imagePaths),null);
        }
    }
}
