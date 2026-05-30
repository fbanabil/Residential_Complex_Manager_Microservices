using ResidentialAreas.API.Helpers.LocationValidator;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Unit
{
    public class AddNewAreaValidatorTests
    {
        private static AddNewAreaRequestValidator BuildSut(bool locationIsValid = true)
        {
            var loc = new Mock<ILocationValidator>();
            loc.Setup(l => l.IsValidLocationAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(locationIsValid);
            return new AddNewAreaRequestValidator(loc.Object);
        }

        private static AddNewAreaRequest Valid() => new(
            Name: "Lakeside Towers", City: "Dhaka", State: "Dhaka", Country: "BD",
            PostalCode: "1207", Address: "12 Lake Rd", GeoBoundary: "{\"type\":\"Polygon\"}",
            Status: "Active",
            ImageBase64: new List<string?> { TestConfigurationFactory.ValidBase64Png });

        [Fact]
        public async Task Accepts_a_well_formed_request()
        {
            var r = await BuildSut().ValidateAsync(Valid());
            r.IsValid.Should().BeTrue(string.Join("; ", r.Errors.Select(e => e.ErrorMessage)));
        }

        [Fact]
        public async Task Rejects_when_location_validator_returns_false()
        {
            var r = await BuildSut(locationIsValid: false).ValidateAsync(Valid());
            r.IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("active")] // case-sensitive: IsEnumName by default rejects lowercase
        [InlineData("")]
        public async Task Rejects_unknown_or_wrongly_cased_status(string status)
        {
            var r = await BuildSut().ValidateAsync(Valid() with { Status = status });
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_overlong_name()
        {
            var r = await BuildSut().ValidateAsync(Valid() with { Name = new string('A', 151) });
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_overlong_postal_code()
        {
            var r = await BuildSut().ValidateAsync(Valid() with { PostalCode = new string('1', 21) });
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_empty_image_list()
        {
            var r = await BuildSut().ValidateAsync(Valid() with { ImageBase64 = new List<string?>() });
            r.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_invalid_image_in_list()
        {
            var r = await BuildSut().ValidateAsync(Valid() with
            {
                ImageBase64 = new List<string?> { "not-base64" }
            });
            r.IsValid.Should().BeFalse();
        }
    }
}
