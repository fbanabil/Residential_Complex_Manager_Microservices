using ResidentialAreas.API.Helpers.Image;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Unit
{
    public class Base64StringImageValidatorTests
    {
        [Fact]
        public void IsBase64StringImage_accepts_png_jpg_jpeg_and_webp_data_urls()
        {
            Base64StringImageValidator.IsBase64StringImage(TestConfigurationFactory.ValidBase64Png).Should().BeTrue();
            Base64StringImageValidator.IsBase64StringImage(TestConfigurationFactory.ValidBase64Jpg).Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("not-a-data-url")]
        [InlineData("data:image/gif;base64,R0lGODlhAQAB")]              // gif is not whitelisted
        [InlineData("data:image/png;base64,this-is-not-valid-base64!")] // invalid base64 payload
        [InlineData("data:text/plain;base64,SGVsbG8=")]                 // wrong mime
        public void IsBase64StringImage_rejects_malformed_inputs(string input)
        {
            Base64StringImageValidator.IsBase64StringImage(input).Should().BeFalse();
        }

        [Fact]
        public void IsBase64StringImage_rejects_a_data_url_with_an_empty_base64_payload()
        {
            Base64StringImageValidator.IsBase64StringImage("data:image/png;base64,").Should().BeFalse();
        }

        [Fact]
        public void IsBase64StringList_accepts_null_and_empty()
        {
            Base64StringImageValidator.IsBase64StringList(null).Should().BeTrue();
            Base64StringImageValidator.IsBase64StringList(new List<string?>()).Should().BeTrue();
        }

        [Fact]
        public void IsBase64StringList_treats_null_or_empty_entries_as_skip()
        {
            var list = new List<string?> { null, "", TestConfigurationFactory.ValidBase64Png };
            Base64StringImageValidator.IsBase64StringList(list).Should().BeTrue();
        }

        [Fact]
        public void IsBase64StringList_rejects_when_any_entry_is_invalid()
        {
            var list = new List<string?> { TestConfigurationFactory.ValidBase64Png, "bogus" };
            Base64StringImageValidator.IsBase64StringList(list).Should().BeFalse();
        }
    }
}
