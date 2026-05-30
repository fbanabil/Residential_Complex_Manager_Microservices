extern alias AuthApi;
using AuthApi::AuthenticationService.API.Apis.User.AddNewUser;
using AuthApi::AuthenticationService.API.Apis.User.ChangePassword;
using AuthApi::AuthenticationService.API.Apis.User.LocalLogin;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.AuthenticationService.Unit
{
    public class LocalLoginRequestValidatorTests
    {
        private readonly LocalLoginRequestValidator _sut = new();

        [Theory]
        [InlineData("", "password123")]
        [InlineData("not-an-email", "password123")]
        [InlineData("user@example.com", "")]
        [InlineData("user@example.com", "12345")]
        public async Task Rejects_invalid_email_or_short_password(string email, string password)
        {
            var result = await _sut.ValidateAsync(new LocalLoginRequest(email, password));
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Accepts_valid_email_and_password_at_minimum_length()
        {
            var result = await _sut.ValidateAsync(new LocalLoginRequest("user@example.com", "123456"));
            result.IsValid.Should().BeTrue();
        }
    }

    public class RegisterUserRequestValidatorTests
    {
        private readonly RegisterUserRequestValidator _sut = new();

        private static RegisterUserRequest Valid() => new(
            UserName: "alice",
            Email: "alice@example.com",
            Password: "Str0ng!Pwd",
            ConfirmPassword: "Str0ng!Pwd",
            Phone: "+8801712345678",
            Bas64ProfileImage: TestConfigurationFactory.ValidBase64Png,
            Base64NidImage: TestConfigurationFactory.ValidBase64Png);

        [Fact]
        public async Task Accepts_well_formed_registration()
        {
            var result = await _sut.ValidateAsync(Valid());
            result.IsValid.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }

        [Fact]
        public async Task Rejects_password_without_uppercase()
        {
            var r = Valid() with { Password = "weak!pwd123", ConfirmPassword = "weak!pwd123" };
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_password_without_digit()
        {
            var r = Valid() with { Password = "Weak!Password!", ConfirmPassword = "Weak!Password!" };
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_password_without_special_char()
        {
            var r = Valid() with { Password = "Weakpwd1234", ConfirmPassword = "Weakpwd1234" };
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_password_shorter_than_8()
        {
            var r = Valid() with { Password = "S!a1bcd", ConfirmPassword = "S!a1bcd" };
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_confirm_password_mismatch()
        {
            var r = Valid() with { ConfirmPassword = "Different!1A" };
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("not-a-phone")]
        [InlineData("0123456789")] // E.164 disallows leading 0 without +
        [InlineData("")]
        public async Task Rejects_invalid_phone(string phone)
        {
            var r = Valid() with { Phone = phone };
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_invalid_base64_profile_image()
        {
            var r = Valid() with { Bas64ProfileImage = "not-base64-image" };
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_invalid_email()
        {
            var r = Valid() with { Email = "alice-at-example-dot-com" };
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }
    }

    public class ChangePasswordValidatorTests
    {
        private readonly ChangePasswordValidator _sut = new();

        [Fact]
        public async Task Accepts_strong_new_password_matching_confirm()
        {
            var r = new ChangePasswordRequest("anyCurrent", "Str0ng!Pwd", "Str0ng!Pwd");
            (await _sut.ValidateAsync(r)).IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task Rejects_mismatched_confirm()
        {
            var r = new ChangePasswordRequest("anyCurrent", "Str0ng!Pwd", "Other!Pwd1");
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_empty_current_password()
        {
            var r = new ChangePasswordRequest("", "Str0ng!Pwd", "Str0ng!Pwd");
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_weak_new_password_missing_special_char()
        {
            var r = new ChangePasswordRequest("any", "Weakpwd1234", "Weakpwd1234");
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Rejects_new_password_shorter_than_eight_characters()
        {
            var r = new ChangePasswordRequest("any", "A1a!", "A1a!");
            (await _sut.ValidateAsync(r)).IsValid.Should().BeFalse();
        }
    }
}
