extern alias AuthApi;
using AuthApi::AuthenticationService.API.Helpers.PasswordHelper.RandomPassword;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class RandomPasswordGeneratorTests
    {
        private const string Alphabet =
            "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*?_-!";

        [Fact]
        public async Task Generate_returns_requested_length()
        {
            var pwd = await RandomPasswordGenerator.Generate(16);
            pwd.Should().HaveLength(16);
        }

        [Fact]
        public async Task Generate_uses_default_length_of_12()
        {
            var pwd = await RandomPasswordGenerator.Generate();
            pwd.Should().HaveLength(12);
        }

        [Fact]
        public async Task Generate_can_select_every_distinct_character_in_the_alphabet_over_a_long_sample()
        {
            var distinctInAlphabet = new HashSet<char>(Alphabet);
            var sampled = new HashSet<char>();
            for (int i = 0; i < 500; i++)
            {
                var pwd = await RandomPasswordGenerator.Generate(256);
                foreach (var c in pwd) sampled.Add(c);
            }
            sampled.Should().BeEquivalentTo(distinctInAlphabet);
        }

        [Fact]
        public async Task Generate_only_uses_characters_from_the_declared_alphabet()
        {
            var pwd = await RandomPasswordGenerator.Generate(200);
            foreach (var c in pwd)
            {
                Alphabet.Should().Contain(c.ToString());
            }
        }

        [Fact]
        public async Task Generate_returns_distinct_values_across_rapid_successive_calls()
        {
            var results = new HashSet<string>();
            for (int i = 0; i < 50; i++)
            {
                results.Add(await RandomPasswordGenerator.Generate(20));
            }
            results.Should().HaveCount(50);
        }
    }
}
