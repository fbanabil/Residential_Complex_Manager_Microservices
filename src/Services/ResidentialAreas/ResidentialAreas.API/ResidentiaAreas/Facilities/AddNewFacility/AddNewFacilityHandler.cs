using ResidentialAreas.API.Helpers.ErrorCarrier;
using ResidentialAreas.API.Helpers.ImageSaver;
using System.Security.Claims;

namespace ResidentialAreas.API.ResidentiaAreas.Facilities.AddNewFacility
{
    public record AddNewFacilityCommand(long? AreaCode, long? BuildingCode, string Name, string FacilityType, int Capacity, bool BookingRequired, decimal? HourlyRate, string? Rules, string Status, List<string?>? ImageBase64) : ICommand<AddNewFacilityResult>;
    public record AddNewFacilityResult(AddNewFacilityResponse? Result, ErrorCarrier? Error);

    public class AddNewFacilityHandler : ICommandHandler<AddNewFacilityCommand, AddNewFacilityResult>
    {
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AddNewFacilityHandler> _logger;
        private readonly IImageSaver _imageSaver;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AddNewFacilityHandler(AreaDbContext areaDbContext, ILogger<AddNewFacilityHandler> logger, IImageSaver imageSaver, IHttpContextAccessor httpContextAccessor)
        {
            _areaDbContext = areaDbContext;
            _logger = logger;
            _imageSaver = imageSaver;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AddNewFacilityResult> Handle(AddNewFacilityCommand request, CancellationToken cancellationToken)
        {
            // Validation: Ensure that either AreaCode or BuildingCode is provided, but not both
            if (!request.AreaCode.HasValue && !request.BuildingCode.HasValue)
            {
                _logger.LogWarning("Create facility failed. Neither AreaCode nor BuildingCode was provided.");
                return new AddNewFacilityResult(null, new ErrorCarrier
                {
                    Title = "VALIDATION_ERROR",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = "Either AreaCode or BuildingCode must be provided, but not both."
                });
            }


            // Validation: Ensure that both AreaCode and BuildingCode are not provided at the same time
            if (request.AreaCode.HasValue && request.BuildingCode.HasValue)
            {
                _logger.LogWarning("Create facility failed. Both AreaCode and BuildingCode were provided.");
                return new AddNewFacilityResult(null, new ErrorCarrier
                {
                    Title = "VALIDATION_ERROR",
                    StatusCode = StatusCodes.Status400BadRequest,
                    Detail = "Both AreaCode and BuildingCode cannot be provided at the same time. Please provide only one."
                });
            }




            // Fetch the associated Area or Building based on the provided codes
            Area? area = null;
            Building? building = null;

            // If AreaCode is provided, fetch the Area
            if (request.AreaCode.HasValue)
            {
                area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode.Value, cancellationToken);
                if (area == null)
                {
                    _logger.LogWarning("Area with code {AreaCode} not found while creating facility.", request.AreaCode.Value);
                    return new AddNewFacilityResult(null, new ErrorCarrier
                    {
                        Title = "NOT_FOUND",
                        StatusCode = StatusCodes.Status404NotFound,
                        Detail = $"Area with code {request.AreaCode.Value} not found."
                    });
                }
            }

            // If BuildingCode is provided, fetch the Building
            if (request.BuildingCode.HasValue)
            {
                building = await _areaDbContext.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Code == request.BuildingCode.Value, cancellationToken);
                if (building == null)
                {
                    _logger.LogWarning("Building with code {BuildingCode} not found while creating facility.", request.BuildingCode.Value);
                    return new AddNewFacilityResult(null, new ErrorCarrier
                    {
                        Title = "NOT_FOUND",
                        StatusCode = StatusCodes.Status404NotFound,
                        Detail = $"Building with code {request.BuildingCode.Value} not found."
                    });
                }
            }



            // Authorization: Check user roles and permissions
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID claim not found.");
            var userRoles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList() ?? new List<string>();



            // Determine if the user is an Admin, Complex Manager, or Tenant based on their roles
            bool isAdmin = userRoles.Contains("Admin");
            bool isComplexManager = userRoles.Contains("AreaManager");
            bool isTenant = userRoles.Contains("Tenant");


            // Authorization: If the user is not an Admin, check if they are a Complex Manager or Tenant and verify their permissions
            if (!isAdmin)
            {
                // Complex Managers can only create facilities in areas they manage
                if (isComplexManager)
                {
                    if(area != null && area.ComplexManagerId != Guid.Parse(userIdClaim.Value))
                    {
                        _logger.LogWarning("Unauthorized access attempt by user {UserId} to create facility in area {AreaCode}.", userIdClaim.Value, request.AreaCode);
                        return new AddNewFacilityResult(null, new ErrorCarrier
                        {
                            Title = "FORBIDDEN",
                            StatusCode = StatusCodes.Status403Forbidden,
                            Detail = "You do not have permission to create a facility in this area."
                        });
                    }
                }

                // Tenants can only create facilities in buildings they are associated with
                if (isTenant)
                {
                    if (building != null && building.TenantId != Guid.Parse(userIdClaim.Value))
                    {
                        _logger.LogWarning("Unauthorized access attempt by user {UserId} to create facility in building {BuildingCode}.", userIdClaim.Value, request.BuildingCode);
                        return new AddNewFacilityResult(null, new ErrorCarrier
                        {
                            Title = "FORBIDDEN",
                            StatusCode = StatusCodes.Status403Forbidden,
                            Detail = "You do not have permission to create a facility in this building."
                        });
                    }
                }
            }





            // Begin a database transaction to ensure atomicity of the operation
            await using var transaction = await _areaDbContext.Database.BeginTransactionAsync(cancellationToken);



            // Save the facility images and get their paths. If saving fails, roll back the transaction and return an error.
            List<string?>? imagePaths = new List<string?>();
            try
            {
                imagePaths = await _imageSaver.SaveImageAsync(request.ImageBase64, "wwwroot/images/Facilities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save facility images for facility '{FacilityName}'", request.Name);
                await transaction.RollbackAsync(cancellationToken);
                return new AddNewFacilityResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while saving the facility images. Please try again."
                });
            }



            // Create a new Facility entity and populate its properties based on the request and the associated Area or Building
            Facility facility = new Facility
            {
                Id = Guid.NewGuid(),
                AreaId = area?.Id,
                BuildingId = building?.Id,
                Name = request.Name,
                FacilityType = request.FacilityType,
                Capacity = request.Capacity,
                BookingRequired = request.BookingRequired,
                HourlyRate = request.HourlyRate,
                Rules = request.Rules,
                Status = System.Enum.Parse<Status>(request.Status, true),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };



            // Save the new Facility to the database. If saving fails, roll back the transaction and return an error.
            try
            {
                await _areaDbContext.Facilities.AddAsync(facility, cancellationToken);
                await _areaDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save new facility '{FacilityName}' to the database", request.Name);
                await transaction.RollbackAsync(cancellationToken);
                return new AddNewFacilityResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while creating the facility. Please try again."
                });
            }



            // If the facility was created successfully and there are images to associate with it
            if (facility.FacilityCode.HasValue && imagePaths != null && imagePaths.Count > 0)
            {
                List<Image> images = imagePaths.Select(path => new Image
                {
                    Id = Guid.NewGuid(),
                    Url = path,
                    ImageType = ImageType.Facility,
                    FacilityCode = facility.FacilityCode.Value
                }).ToList();



                // Save the new Images to the database. If saving fails, roll back the transaction and return an error.
                try
                {
                    await _areaDbContext.Images.AddRangeAsync(images, cancellationToken);
                    await _areaDbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save image records for facility code {FacilityCode}", facility.FacilityCode);
                    await transaction.RollbackAsync(cancellationToken);
                    return new AddNewFacilityResult(null, new ErrorCarrier
                    {
                        Title = "INTERNAL_SERVER_ERROR",
                        StatusCode = StatusCodes.Status500InternalServerError,
                        Detail = "An error occurred while saving the facility images. Please try again."
                    });
                }
            }




            // If everything was successful, commit the transaction. If committing fails, roll back the transaction and return an error.
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit transaction for new facility '{FacilityName}'", request.Name);
                await transaction.RollbackAsync(cancellationToken);
                return new AddNewFacilityResult(null, new ErrorCarrier
                {
                    Title = "INTERNAL_SERVER_ERROR",
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while finalizing the facility creation. Please try again."
                });
            }


            // Convert the saved image paths to URLs that can be accessed by clients.
            var httpContext = _httpContextAccessor.HttpContext;
            imagePaths = imagePaths?.Select(path => $"{httpContext?.Request.Scheme}://{httpContext?.Request.Host}/{path}").ToList()!;


            _logger.LogInformation("Facility '{FacilityName}' created successfully with code {FacilityCode}", request.Name, facility.FacilityCode);
            // Return the details of the newly created Facility in the response, along with any associated Area or Building information and the image URLs.
            return new AddNewFacilityResult(new AddNewFacilityResponse
                (
                    facility.Id!.Value,
                    facility.FacilityCode ?? 0,
                    facility.Name ?? string.Empty,
                    facility.FacilityType ?? string.Empty,
                    facility.Capacity ?? 0,
                    facility.BookingRequired ?? false,
                    facility.HourlyRate,
                    facility.Rules,
                    facility.Status.ToString(),
                    area?.Code,
                    area?.Name,
                    building?.Code,
                    building?.Name,
                    imagePaths), null
                );
        }
    }
}
