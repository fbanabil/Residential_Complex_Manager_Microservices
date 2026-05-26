using ResidentialAreas.API.Grpc;
using ResidentialAreas.API.Helpers.ErrorCarrier;

namespace ResidentialAreas.API.ResidentiaAreas.Areas.AssignComplexManager
{
    public record AssignComplexManagerCommand(long AreaCode, string Email) : ICommand<AssignComplexManagerResult>;
    public record AssignComplexManagerResult(AssignComplexManagerResponse? Result, ErrorCarrier? Error);

    public class AssignComplexManagerHandler : ICommandHandler<AssignComplexManagerCommand, AssignComplexManagerResult>
    {
        private readonly UserValidations.UserValidationsClient _userValidationsClient;
        private readonly AreaDbContext _areaDbContext;
        private readonly ILogger<AssignComplexManagerHandler> _logger;

        public AssignComplexManagerHandler(UserValidations.UserValidationsClient userValidationsClient, AreaDbContext areaDbContext, ILogger<AssignComplexManagerHandler> logger)
        {
            _userValidationsClient = userValidationsClient;
            _areaDbContext = areaDbContext;
            _logger = logger;
        }

        public async Task<AssignComplexManagerResult> Handle(AssignComplexManagerCommand request, CancellationToken cancellationToken)
        {
            // Validate the user using gRPC
            GetUserResponse user = await _userValidationsClient.GetUserAsync(new GetUserRequest { Email = request.Email }, cancellationToken: cancellationToken);


            // Check for gRPC errors
            int errorStatusCode = int.TryParse(user.Error.StatusCode, out int statusCode) ? statusCode : 500;


            // Handle gRPC errors
            if (errorStatusCode != 200)
            {
                _logger.LogWarning("Assign complex manager failed: gRPC error {StatusCode} for email {Email}", errorStatusCode, request.Email);
                return new AssignComplexManagerResult(null, new ErrorCarrier { StatusCode = errorStatusCode, Detail = user.Error.ErrorMessage, Title = user.Error.Title });
            }



            // Validate user existence and role
            if (user.UserByRpc == null)
            {
                _logger.LogWarning("Assign complex manager failed: user not found for email {Email}", request.Email);
                return new AssignComplexManagerResult(null, new ErrorCarrier { StatusCode = 404, Detail = "User not found", Title = "USER_NOT_FOUND" });
            }


            // Check if the user is verified
            if (user.UserByRpc.IsUserVerified == false)
            {
                _logger.LogWarning("Assign complex manager failed: user {Email} is not verified", request.Email);
                return new AssignComplexManagerResult(null, new ErrorCarrier { StatusCode = 400, Detail = "User is not verified", Title = "UNVERIFIED_USER" });
            }


            // Check if the user has the Complex Manager role
            if (!user.UserByRpc.Role.Contains("ComplexManager"))
            {
                _logger.LogWarning("Assign complex manager failed: user {Email} does not have the ComplexManager role", request.Email);
                return new AssignComplexManagerResult(null, new ErrorCarrier { StatusCode = 400, Detail = "User is not a Complex Manager", Title = "INVALID_USER_ROLE" });
            }

            // Parse the user ID from the gRPC response
            Guid userId = Guid.TryParse(user.UserByRpc.Id, out Guid parsedUserId) ? parsedUserId : Guid.Empty;
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("Assign complex manager failed: invalid user ID in gRPC response for email {Email}", request.Email);
                return new AssignComplexManagerResult(null, new ErrorCarrier { StatusCode = 400, Detail = "Invalid user ID", Title = "INVALID_USER_ID" });
            }



            // Check if the area exists
            Area? area = await _areaDbContext.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Code == request.AreaCode, cancellationToken);
            if (area == null)
            {
                _logger.LogWarning("Assign complex manager failed: no area found with code {AreaCode}", request.AreaCode);
                return new AssignComplexManagerResult(null, new ErrorCarrier { StatusCode = 404, Detail = "Area not found", Title = "AREA_NOT_FOUND" });
            }



            // Update the Complex Manager for the area
            try
            {
                await _areaDbContext.Areas.Where(a => a.Code == request.AreaCode)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.ComplexManagerId, userId), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Assign complex manager failed: database error for area code {AreaCode} and email {Email}", request.AreaCode, request.Email);
                return new AssignComplexManagerResult(null, new ErrorCarrier
                {
                    StatusCode = 500, Detail = ex.Message, Title = "INTERNAL_SERVER_ERROR"
                });
            }

            _logger.LogInformation("Complex Manager {Email} assigned successfully to area code {AreaCode}", request.Email, request.AreaCode);
            return new AssignComplexManagerResult(new AssignComplexManagerResponse(Success: true, Message:"Complex Manager assigned successfully"), null);
        }
    }
}
