using ResidentialAreas.API.Helpers.Image;
using ResidentialAreas.API.Helpers.LocationValidator;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;
using Residential_Complex_Manager_Tests.Common;
using System.Diagnostics;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Performance
{
    public class AreaPerformanceTests
    {
        [Fact]
        public void Base64StringImageValidator_processes_50k_valid_strings_under_two_seconds()
        {
            var input = TestConfigurationFactory.ValidBase64Png;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 50_000; i++)
                Base64StringImageValidator.IsBase64StringImage(input);
            sw.Stop();
            sw.Elapsed.TotalSeconds.Should().BeLessThan(2);
        }

        [Fact]
        public async Task AddNewAreaValidator_under_500ms_for_100_runs_with_passthrough_location()
        {
            var loc = new Mock<ILocationValidator>();
            loc.Setup(l => l.IsValidLocationAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var v = new AddNewAreaRequestValidator(loc.Object);
            var req = new AddNewAreaRequest("X", "C", "S", "BD", "1207", "addr",
                "{}", "Active", new List<string?> { TestConfigurationFactory.ValidBase64Png });

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++) await v.ValidateAsync(req);
            sw.Stop();
            sw.ElapsedMilliseconds.Should().BeLessThan(500);
        }
    }
}
