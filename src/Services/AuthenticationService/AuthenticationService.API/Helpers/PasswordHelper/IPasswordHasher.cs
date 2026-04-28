namespace AuthenticationService.API.Helpers.PasswordHelper
{
    public interface IPasswordHasher
    {
        public Task<string> HashPassword(string password);
        public Task<bool> VerifyPassword(string password, string hashedPassword);

    }
}
