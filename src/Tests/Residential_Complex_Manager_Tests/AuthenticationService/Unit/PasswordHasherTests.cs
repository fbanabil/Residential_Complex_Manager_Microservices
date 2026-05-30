extern alias AuthApi;
using AuthApi::AuthenticationService.API.Helpers.PasswordHelper.Hasher;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class PasswordHasherTests
    {
        private static PasswordHasher Hasher(string? pepper = "TEST_PEPPER")
            => new(TestConfigurationFactory.BuildAuthConfiguration(pepper));

        [Fact]
        public async Task HashPassword_produces_non_empty_hash_distinct_from_input()
        {
            var hasher = Hasher();
            var hash = await hasher.HashPassword("Str0ng!Passw0rd");
            hash.Should().NotBeNullOrWhiteSpace();
            hash.Should().NotBe("Str0ng!Passw0rd");
        }

        [Fact]
        public async Task HashPassword_is_non_deterministic_due_to_bcrypt_salt()
        {
            var hasher = Hasher();
            var a = await hasher.HashPassword("samePassword!1");
            var b = await hasher.HashPassword("samePassword!1");
            a.Should().NotBe(b, "BCrypt salts every hash; two hashes of the same password must differ");
        }

        [Fact]
        public async Task VerifyPassword_returns_true_for_correct_password()
        {
            var hasher = Hasher();
            var hash = await hasher.HashPassword("Correct!Horse9");
            (await hasher.VerifyPassword("Correct!Horse9", hash)).Should().BeTrue();
        }

        [Fact]
        public async Task VerifyPassword_returns_false_for_incorrect_password()
        {
            var hasher = Hasher();
            var hash = await hasher.HashPassword("Correct!Horse9");
            (await hasher.VerifyPassword("Wrong!Horse9", hash)).Should().BeFalse();
        }

        [Fact]
        public async Task VerifyPassword_fails_when_pepper_differs_between_hash_and_verify()
        {
            var hashedWithPepperA = await Hasher("PEPPER_A").HashPassword("p@ssw0rdZ");
            var verifyWithPepperB = await Hasher("PEPPER_B").VerifyPassword("p@ssw0rdZ", hashedWithPepperA);
            verifyWithPepperB.Should().BeFalse("a different pepper invalidates the hash by design");
        }

        [Fact]
        public async Task VerifyPassword_round_trips_when_no_pepper_is_configured()
        {
            var hasher = Hasher(pepper: null);
            var hash = await hasher.HashPassword("anything!1A");
            (await hasher.VerifyPassword("anything!1A", hash)).Should().BeTrue();
        }
    }
}
