extern alias AuthApi;
using AuthApi::AuthenticationService.API.Helpers.Authenticate;
using AuthApi::AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using AuthApi::AuthenticationService.API.Helpers.RefreashTokenHelper;
using AuthApi::AuthenticationService.API.Helpers.VerificationToken;
using Residential_Complex_Manager_Tests.Common;
using System.Diagnostics;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Performance
{
    /// <summary>
    /// Micro-benchmarks for the security primitives. These assertions are deliberately
    /// generous so they don't flake on slow CI, but they catch order-of-magnitude regressions.
    /// </summary>
    public class AuthPerformanceTests
    {
        [Fact]
        public async Task HashPassword_completes_within_three_seconds_for_one_iteration()
        {
            var hasher = new PasswordHasher(TestConfigurationFactory.BuildAuthConfiguration());
            var sw = Stopwatch.StartNew();
            await hasher.HashPassword("Sup3r!Strong");
            sw.Stop();
            sw.ElapsedMilliseconds.Should().BeLessThan(3000,
                "BCrypt with work factor 12 should typically finish well under a second on dev hardware");
        }

        [Fact]
        public async Task VerifyPassword_throughput_at_least_4_per_second()
        {
            var hasher = new PasswordHasher(TestConfigurationFactory.BuildAuthConfiguration());
            var hash = await hasher.HashPassword("Sup3r!Strong");
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 4; i++)
            {
                await hasher.VerifyPassword("Sup3r!Strong", hash);
            }
            sw.Stop();
            sw.ElapsedSeconds().Should().BeLessThan(4);
        }

        [Fact]
        public async Task CreateJwt_completes_in_under_500ms_for_one_token()
        {
            var creator = new AuthenticationTokenCreator(TestConfigurationFactory.BuildAuthConfiguration());
            var payload = new UserPayload(Guid.NewGuid().ToString(), "u", "u@x.com", new List<string> { "User" });
            var sw = Stopwatch.StartNew();
            await creator.CreateToken(payload);
            sw.Stop();
            sw.ElapsedMilliseconds.Should().BeLessThan(500);
        }

        [Fact]
        public async Task RefreshTokenGeneration_1000_tokens_under_2_seconds()
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                _ = await RefreashTokenGenerator.CreateTokenAsync();
            }
            sw.Stop();
            sw.ElapsedSeconds().Should().BeLessThan(2);
        }

        [Fact]
        public async Task VerificationTokenHash_5000_under_1_second()
        {
            var sut = new VerificationTokenGenerator();
            var token = await sut.GenerateTokenAsync();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 5000; i++) await sut.HashTokenAsync(token);
            sw.Stop();
            sw.ElapsedSeconds().Should().BeLessThan(1);
        }
    }

    internal static class StopwatchExtensions
    {
        public static double ElapsedSeconds(this Stopwatch sw) => sw.Elapsed.TotalSeconds;
    }
}
