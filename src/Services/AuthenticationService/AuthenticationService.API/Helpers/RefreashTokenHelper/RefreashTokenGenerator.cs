using System.Security.Cryptography;

namespace AuthenticationService.API.Helpers.RefreashTokenHelper
{
    public static class RefreashTokenGenerator
    {
        public async static Task<string> CreateTokenAsync()
        {

            var bytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
            return await Task.FromResult(token);
        }

        public async static Task<string> HashTokenAsync(string token)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            var hashedToken = Convert.ToBase64String(hashBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
            return await Task.FromResult(hashedToken);
        }
    
        public static async Task<bool> VerifyTokenAsync(string token, string hashedToken)
        {
            var expectedHashedToken = await HashTokenAsync(token);
    
            return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(expectedHashedToken),
                System.Text.Encoding.UTF8.GetBytes(hashedToken)
            );
        }
    }
}
