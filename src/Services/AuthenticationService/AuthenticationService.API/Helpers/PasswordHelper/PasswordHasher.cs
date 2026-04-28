namespace AuthenticationService.API.Helpers.PasswordHelper
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly IConfiguration _configuration;

        public PasswordHasher(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<string> HashPassword(string password)
        {
            string pepper = _configuration["Security:PasswordPepper"] ?? string.Empty;
            string passwordWithPepper = password + pepper;
            string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(passwordWithPepper, 12);
            return Task.FromResult(hashedPassword);
        }


        public Task<bool> VerifyPassword(string password, string hashedPassword)
        {
            string pepper = _configuration["Security:PasswordPepper"] ?? string.Empty;
            string passwordWithPepper = password + pepper;
            bool isValid = BCrypt.Net.BCrypt.EnhancedVerify(passwordWithPepper, hashedPassword);
            return Task.FromResult(isValid);

        }
    }
}
