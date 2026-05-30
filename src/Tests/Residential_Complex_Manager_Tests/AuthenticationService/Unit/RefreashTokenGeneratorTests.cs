extern alias AuthApi;
using AuthApi::AuthenticationService.API.Helpers.RefreashTokenHelper;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class RefreashTokenGeneratorTests
    {
        [Fact]
        public async Task CreateToken_is_url_safe_and_unique()
        {
            var tokens = new HashSet<string>();
            for (int i = 0; i < 500; i++)
            {
                var t = await RefreashTokenGenerator.CreateTokenAsync();
                t.Should().NotContain("+").And.NotContain("/").And.NotEndWith("=");
                tokens.Add(t);
            }
            tokens.Should().HaveCount(500);
        }

        [Fact]
        public async Task HashToken_is_stable_for_same_input()
        {
            var token = await RefreashTokenGenerator.CreateTokenAsync();
            var h1 = await RefreashTokenGenerator.HashTokenAsync(token);
            var h2 = await RefreashTokenGenerator.HashTokenAsync(token);
            h1.Should().Be(h2);
        }

        [Fact]
        public async Task VerifyToken_returns_true_for_matching_pair()
        {
            var token = await RefreashTokenGenerator.CreateTokenAsync();
            var hash = await RefreashTokenGenerator.HashTokenAsync(token);
            (await RefreashTokenGenerator.VerifyTokenAsync(token, hash)).Should().BeTrue();
        }

        [Fact]
        public async Task VerifyToken_returns_false_for_mismatched_pair()
        {
            var token = await RefreashTokenGenerator.CreateTokenAsync();
            var other = await RefreashTokenGenerator.CreateTokenAsync();
            var hash = await RefreashTokenGenerator.HashTokenAsync(other);
            (await RefreashTokenGenerator.VerifyTokenAsync(token, hash)).Should().BeFalse();
        }
    }
}
