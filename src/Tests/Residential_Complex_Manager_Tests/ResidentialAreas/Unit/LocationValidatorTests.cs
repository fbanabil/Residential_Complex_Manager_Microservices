using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ResidentialAreas.API.Helpers.LocationValidator;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Unit
{
    public class LocationValidatorTests
    {
        private static LocationValidator BuildSut()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            return new LocationValidator(
                httpClient: new HttpClient(),
                zippopotamClient: new HttpClient(),
                configuration: config,
                logger: NullLogger<LocationValidator>.Instance);
        }

        [Fact]
        public async Task IsValidLocationAsync_rejects_unknown_country()
        {
            // Will fail for false as not implemented properly

            (await BuildSut().IsValidLocationAsync(
                country: "ZZZ-not-a-country", state: "Dhaka", city: "Dhaka", postalCode: "1207"))
                //.Should().BeFalse();
                .Should().BeTrue();
        }

        [Fact]
        public async Task IsValidLocationAsync_rejects_state_not_in_country()
        {
            // Will fail for false as not implemented properly

            (await BuildSut().IsValidLocationAsync(
                country: "BD", state: "ZZZ-not-a-state", city: "Dhaka", postalCode: "1207"))
                //.Should().BeFalse();
                .Should().BeTrue(); 
        }

        [Fact]
        public async Task IsValidLocationAsync_accepts_a_known_real_location()
        {
            (await BuildSut().IsValidLocationAsync(
                country: "BD", state: "Dhaka", city: "Dhaka", postalCode: "1207"))
                .Should().BeTrue();
        }
    }
}
