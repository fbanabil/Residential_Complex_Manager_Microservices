using System.Security.Cryptography;
using System.Text;

namespace AuthenticationService.API.Helpers.VerificationToken
{
    public class VerificationTokenGenerator : IVerificationTokenGenerator
    {
        public Task<string> GenerateTokenAsync()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            return Task.FromResult(token);
        }

        public Task<string> HashTokenAsync(string token)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            var hashedToken = Convert.ToBase64String(hashBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
            return Task.FromResult(hashedToken);
        }


        public async Task<bool> VerifyTokenAsync(string token, string hashedToken)
        {
            var expectedHashedToken = await HashTokenAsync(token);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHashedToken),
                Encoding.UTF8.GetBytes(hashedToken)
            );
        }
    }
}
