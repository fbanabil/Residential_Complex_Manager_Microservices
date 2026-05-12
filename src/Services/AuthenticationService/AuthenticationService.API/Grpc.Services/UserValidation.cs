using AuthenticationService.API.AuthenticationDbContest;
using AuthenticationService.API.Grpc;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AuthenticationService.API.Grpc.Services
{
    public class UserValidations : AuthenticationService.API.Grpc.UserValidations.UserValidationsBase
    {
        private readonly AuthDbContext _dbContext;
        private readonly ILogger<UserValidations> _logger;

        public UserValidations(AuthDbContext dbContext, ILogger<UserValidations> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }



        public override async Task<GetUserResponse> GetUser(GetUserRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Retrieving user with email: {Email}", request.Email);

                EntityModels.User? user = await _dbContext.Users.AsNoTracking()
                    .Include(r => r.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.ProfileImage)
                    .Include(u => u.NidImage)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found.", request.Email);
                    return new GetUserResponse
                    {
                        UserByRpc = new UserByRpc
                        {
                            Id = string.Empty,
                            Email = string.Empty,
                            Name = string.Empty,
                            ContactNumber = string.Empty,
                            Role = string.Empty,
                            ProfileImageUrl = string.Empty,
                            NidImageUrl = string.Empty,
                            IsEmailConfirmed = false,
                            IsUserVerified = false
                        },
                        Error = new ErrorCarrierRpc
                        {
                            Title = "User Not Found",
                            StatusCode = "404",
                            ErrorMessage = "The requested user does not exist."
                        }
                    };
                }

                var roles = user.UserRoles?
                    .Where(ur => ur.Role != null)
                    .Select(ur => ur.Role!.Name)
                    .ToList() ?? new List<string>();

                return new GetUserResponse
                {
                    UserByRpc = new UserByRpc
                    {
                        Id = user.Id.ToString(),
                        Email = user.Email ?? string.Empty,
                        Name = user.Username ?? string.Empty,
                        ContactNumber = user.Phone ?? string.Empty,
                        Role = string.Join(", ", roles),
                        ProfileImageUrl = user.ProfileImage?.ImagePath ?? string.Empty,
                        NidImageUrl = user.NidImage?.ImagePath ?? string.Empty,
                        IsEmailConfirmed = user.IsEmailVerified,
                        IsUserVerified = user.IsUserVerified
                    },
                    Error = new ErrorCarrierRpc
                    {
                        Title = "Success",
                        StatusCode = "200",
                        ErrorMessage = "User retrieved successfully."
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user with email: {Email}", request.Email);
                return new GetUserResponse
                {
                    UserByRpc = new UserByRpc
                    {
                        Id = string.Empty,
                        Email = string.Empty,
                        Name = string.Empty,
                        ContactNumber = string.Empty,
                        Role = string.Empty,
                        ProfileImageUrl = string.Empty,
                        NidImageUrl = string.Empty
                    },
                    Error = new ErrorCarrierRpc
                    {
                        Title = "Internal Server Error",
                        StatusCode = "500",
                        ErrorMessage = ex.Message
                    }
                };
            }
        }
    }
}
