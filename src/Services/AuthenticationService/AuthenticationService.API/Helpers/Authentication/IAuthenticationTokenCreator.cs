namespace AuthenticationService.API.Helpers.Authenticate
{
    public interface IAuthenticationTokenCreator
    {
        public Task<string> CreateToken(UserPayload payload);

    }
}
