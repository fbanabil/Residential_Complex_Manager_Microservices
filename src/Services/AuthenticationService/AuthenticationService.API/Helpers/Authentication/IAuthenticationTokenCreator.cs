namespace AuthenticationService.API.Helpers.Authorization
{
    public interface IAuthenticationTokenCreator
    {
        public Task<string> CreateToken(UserPayload payload);

    }
}
