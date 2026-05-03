using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthenticationService.API.Helpers.Authorization
{
    public record UserPayload(string UserId, string Username, string Email, List<string> Roles);

    public class AuthenticationTokenCreator : IAuthenticationTokenCreator
    {
        private readonly IConfiguration _configuration;

        public AuthenticationTokenCreator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> CreateToken(UserPayload payload)
        {
            using var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(_configuration["JwtSettings:PrivateKey"]);
            var credentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, payload.UserId),
                new Claim(JwtRegisteredClaimNames.UniqueName, payload.Username),
                new Claim(JwtRegisteredClaimNames.Email, payload.Email),
                new Claim(ClaimTypes.Role, string.Join(",", payload.Roles))
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: credentials
            );
            return await Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }


    }
}
