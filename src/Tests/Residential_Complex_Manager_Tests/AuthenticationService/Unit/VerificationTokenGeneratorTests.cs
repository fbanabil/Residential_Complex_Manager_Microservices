extern alias AuthApi;
using AuthApi::AuthenticationService.API.Helpers.VerificationToken;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class VerificationTokenGeneratorTests
    {
        private readonly VerificationTokenGenerator _sut = new();

        [Fact]
        public async Task GenerateToken_yields_unique_high_entropy_tokens()
        {
            var tokens = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                tokens.Add(await _sut.GenerateTokenAsync());
            }
            tokens.Should().HaveCount(1000, "every random token must be unique");
        }

        [Fact]
        public async Task GenerateToken_is_url_safe_base64()
        {
            var token = await _sut.GenerateTokenAsync();
            token.Should().NotContain("+").And.NotContain("/").And.NotEndWith("=");
            token.Length.Should().BeGreaterThan(20);
        }

        [Fact]
        public async Task HashToken_is_deterministic()
        {
            var token = await _sut.GenerateTokenAsync();
            var h1 = await _sut.HashTokenAsync(token);
            var h2 = await _sut.HashTokenAsync(token);
            h1.Should().Be(h2);
        }

        [Fact]
        public async Task VerifyToken_succeeds_with_matching_token_and_hash()
        {
            var token = await _sut.GenerateTokenAsync();
            var hash = await _sut.HashTokenAsync(token);
            (await _sut.VerifyTokenAsync(token, hash)).Should().BeTrue();
        }

        [Fact]
        public async Task VerifyToken_fails_with_modified_token()
        {
            var token = await _sut.GenerateTokenAsync();
            var hash = await _sut.HashTokenAsync(token);
            var tampered = token.Substring(0, token.Length - 1) + (token[^1] == 'A' ? 'B' : 'A');
            (await _sut.VerifyTokenAsync(tampered, hash)).Should().BeFalse();
        }

        [Fact]
        public async Task VerifyToken_fails_when_hash_is_garbage()
        {
            var token = await _sut.GenerateTokenAsync();
            (await _sut.VerifyTokenAsync(token, "not-the-real-hash")).Should().BeFalse();
        }
    }
}
