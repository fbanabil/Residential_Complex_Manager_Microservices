using Microsoft.AspNetCore.Http;
using ResidentialAreas.API.Helpers.ImageSaver;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Unit
{
    public class ImageSaverTests : IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"img-test-{Guid.NewGuid():N}");
        private readonly ImageSaver _sut;

        public ImageSaverTests()
        {
            _sut = new ImageSaver(new HttpContextAccessor());
        }

        [Fact]
        public async Task SaveImageAsync_writes_a_file_to_disk()
        {
            var dir = Path.Combine(_tempDir, "wwwroot", "images", "areas");
            var path = await _sut.SaveImageAsync(TestConfigurationFactory.ValidBase64Png, dir);

            path.Should().NotBeNullOrEmpty();
            File.Exists(Path.Combine(dir, Path.GetFileName(path))).Should().BeTrue();
        }

        [Fact]
        public async Task SaveImageAsync_returns_a_path_with_the_wwwroot_segment_stripped_on_any_platform()
        {
            var dir = Path.Combine(_tempDir, "wwwroot", "images", "areas");
            var path = await _sut.SaveImageAsync(TestConfigurationFactory.ValidBase64Png, dir);

            path.Should().NotContain("wwwroot");
        }

        [Fact]
        public async Task SaveImageAsync_throws_on_empty_input()
        {
            var act = async () => await _sut.SaveImageAsync(string.Empty, _tempDir);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SaveImageAsync_throws_on_unsupported_mime()
        {
            var act = async () => await _sut.SaveImageAsync(
                "data:image/gif;base64,R0lGODlhAQABAAAAACw=", _tempDir);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SaveImageAsync_throws_on_invalid_base64_payload()
        {
            var act = async () => await _sut.SaveImageAsync(
                "data:image/png;base64,this is not really base64!!!", _tempDir);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SaveImageAsync_list_falls_back_to_default_on_per_item_failure()
        {
            var dir = Path.Combine(_tempDir, "wwwroot", "images", "areas");
            var input = new List<string?>
            {
                TestConfigurationFactory.ValidBase64Png,
                "data:image/gif;base64,xxxx",
                null
            };
            var saved = await _sut.SaveImageAsync(input, dir);
            saved.Should().HaveCount(3);
            saved[1].Should().Be("images/default.jpg");
            saved[2].Should().BeNull();
        }

        [Fact]
        public async Task GetPath_strips_everything_before_the_images_segment()
        {
            (await _sut.GetPath("https://host/wwwroot/images/areas/abc.png")).Should().Be("images/areas/abc.png");
            (await _sut.GetPath("plain-path-no-images.png")).Should().Be("plain-path-no-images.png");
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
