using ResidentialAreas.API.Grpc;
using ResidentialAreas.API.Helpers.ErrorCarrier;

namespace ResidentialAreas.API.ResidentiaAreas.Buildings.AssignTenantToBuildings
{
    public record AssignTenantToBuildingCommand(long BuildingCode, string Email) : ICommand<AssignTenantToBuildingResult>;

    public record AssignTenantToBuildingResult(AssignTenantToBuildingResponse? Result, ErrorCarrier? Error);


    public class AssignTenantToBuildingsHandler : ICommandHandler<AssignTenantToBuildingCommand, AssignTenantToBuildingResult>
    {
        private readonly UserValidations.UserValidationsClient _userValidationsClient;
        private readonly AreaDbContext _areaDbContest;


        public AssignTenantToBuildingsHandler(UserValidations.UserValidationsClient userValidationsClient, AreaDbContext areaDbContest)
        {
            _userValidationsClient = userValidationsClient;
            _areaDbContest = areaDbContest;
        }


        public async Task<AssignTenantToBuildingResult> Handle(AssignTenantToBuildingCommand request, CancellationToken cancellationToken)
        {
            // Validate the user by calling the User Validations gRPC service
            GetUserResponse user = await _userValidationsClient.GetUserAsync(new GetUserRequest { Email = request.Email }, cancellationToken: cancellationToken);

            // Check if the gRPC call was successful and if the user is valid
            int statusCode = int.TryParse(user.Error?.StatusCode, out int code) ? code : 500;
            if (statusCode != 200)
            {
                return new AssignTenantToBuildingResult(null, new ErrorCarrier
                {
                    Title = user.Error?.Title,
                    StatusCode = statusCode,
                    Detail = user.Error?.ErrorMessage
                });
            }



            // Check if the user is verified
            if (user.UserByRpc.IsUserVerified == false)
            {
                return new AssignTenantToBuildingResult(null, new ErrorCarrier
                {
                    Title = "USER_NOT_VERIFIED",
                    StatusCode = 403,
                    Detail = "The user must be verified to be assigned to a building"
                });
            }


            // Retrieve the building from the database using the provided building code
            Building? building = await _areaDbContest.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Code == request.BuildingCode, cancellationToken);
            if (building == null)
            {
                return new AssignTenantToBuildingResult(null, new ErrorCarrier
                {
                    Title = "BUILDING_NOT_FOUND",
                    StatusCode = 404,
                    Detail = $"No building found with code {request.BuildingCode}"
                });
            }



            // Update the building's tenant information in the database
            try
            {
                await _areaDbContest.Buildings.Where(b => b.Id == building.Id)
                    .ExecuteUpdateAsync(b => b.SetProperty(p => p.TenantId, Guid.Parse(user.UserByRpc.Id)), cancellationToken);
            }
            catch
            {
                return new AssignTenantToBuildingResult(null, new ErrorCarrier
                {
                    Title = "DATABASE_ERROR",
                    StatusCode = 500,
                    Detail = "An error occurred while updating the building's tenant information"
                });
            }


            return new AssignTenantToBuildingResult(new AssignTenantToBuildingResponse(Success : true, Message : "Tenant successfully assigned to the building"), null);

        }
    }
}
