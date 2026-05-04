namespace AuthenticationService.API.Helpers.PasswordHelper.RandomPassword
{
    public static class RandomPasswordGenerator
    {
        public async static Task<string> Generate(int length = 12)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*?_-!";
            var random = new Random();
            return await Task.FromResult(new string(Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length-1)]).ToArray()));
        }
    }
}
