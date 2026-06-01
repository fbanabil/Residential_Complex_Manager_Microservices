extern alias AuthApi;
using AuthApi::AuthenticationService.API.Helpers.Authenticate;
using Residential_Complex_Manager_Tests.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class AuthenticationTokenCreatorTests
    {
        private readonly AuthenticationTokenCreator _sut =
            new(TestConfigurationFactory.BuildAuthConfiguration());

        [Fact]
        public async Task CreateToken_produces_a_well_formed_jwt()
        {
            var payload = new UserPayload(Guid.NewGuid().ToString(), "alice", "alice@example.com",
                new List<string> { "Admin", "User" });

            var jwt = await _sut.CreateToken(payload);

            jwt.Should().NotBeNullOrWhiteSpace();
            jwt.Split('.').Should().HaveCount(3);
        }

        [Fact]
        public async Task CreateToken_embeds_expected_email_and_subject_claims_for_the_user()
        {
            var userId = Guid.NewGuid().ToString();
            var payload = new UserPayload(userId, "bob", "bob@example.com",
                new List<string> { "Admin" });

            var jwt = await _sut.CreateToken(payload);

            var json = DecodePayload(jwt);
            json.Should().Contain("\"iss\":\"test-issuer\"");
            json.Should().Contain("\"aud\":\"test-audience\"");
            json.Should().Contain($"\"{JwtRegisteredClaimNames.Sub}\":\"{userId}\"");
            json.Should().Contain("bob@example.com");
        }

        [Fact]
        public async Task CreateToken_emits_one_role_claim_per_role_so_role_based_authorization_can_match()
        {
            var payload = new UserPayload(Guid.NewGuid().ToString(), "carol", "c@example.com",
                new List<string> { "Admin", "User" });

            var jwt = await _sut.CreateToken(payload);
            var json = DecodePayload(jwt);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var roleProp = doc.RootElement.EnumerateObject()
                .First(p => p.Name == ClaimTypes.Role || p.Name == "role" || p.Name == "roles");

            roleProp.Value.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
            roleProp.Value.EnumerateArray().Select(e => e.GetString()).ToList()
                .Should().BeEquivalentTo(new[] { "Admin", "User" });
        }

        [Fact]
        public async Task CreateToken_expires_approximately_one_day_from_now()
        {
            var payload = new UserPayload(Guid.NewGuid().ToString(), "dan", "d@example.com",
                new List<string>());
            var before = DateTimeOffset.UtcNow;
            var jwt = await _sut.CreateToken(payload);
            var json = DecodePayload(jwt);

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var exp = DateTimeOffset.FromUnixTimeSeconds(doc.RootElement.GetProperty("exp").GetInt64());
            exp.Should().BeCloseTo(before.AddDays(1), TimeSpan.FromMinutes(2));
        }

        private static string DecodePayload(string jwt)
        {
            var middle = jwt.Split('.')[1];
            string padded = middle.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        }
    }
}
