namespace AuthenticationService.API.Helpers.NewFolder
{
    public interface IVerificationTokenGenerator
    {
        Task<string> GenerateTokenAsync();
        Task<string> HashTokenAsync(string token);
        public Task<bool> VerifyTokenAsync(string token, string hashedToken);

    }
}
