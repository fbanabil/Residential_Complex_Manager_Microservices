namespace AuthenticationService.API.Helpers.VerificationToken
{
    public interface IVerificationTokenGenerator
    {
        Task<string> GenerateTokenAsync();
        Task<string> HashTokenAsync(string token);
        public Task<bool> VerifyTokenAsync(string token, string hashedToken);

    }
}
