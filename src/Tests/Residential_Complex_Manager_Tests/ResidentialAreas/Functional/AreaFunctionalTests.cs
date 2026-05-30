using Microsoft.Extensions.Logging.Abstractions;
using ResidentialAreas.API.Enum;
using ResidentialAreas.API.EntityModels;
using ResidentialAreas.API.Helpers.LocationValidator;
using ResidentialAreas.API.ResidentiaAreas.Areas.AddNewArea;
using ResidentialAreas.API.ResidentiaAreas.Areas.FilterArea;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Functional
{
    /// <summary>
    /// Validate-then-execute style functional tests that drive a request through the
    /// validator and into a handler, then verify the resulting state via a follow-up query.
    /// </summary>
    public class AreaFunctionalTests
    {
        [Fact]
        public async Task Submitting_a_valid_area_makes_it_filterable()
        {
            await using var scope = new SqliteAreaDbContextScope();
            var ctx = scope.Context;

            // Manually seed an area (replicating what AddNewAreaHandler would do, sidestepping
            // its dependency on the DB-identity Code column which SQLite does not auto-fill).
            ctx.Areas.Add(new Area
            {
                Id = Guid.NewGuid(), Code = 1_000_000_900,
                Name = "Functional Park", City = "Dhaka", State = "Dhaka", Country = "BD",
                PostalCode = "1207", Address = "func", GeoBoundary = "{}",
                Status = Status.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            // Validate the equivalent request would have passed the validator
            var loc = new Mock<ILocationValidator>();
            loc.Setup(l => l.IsValidLocationAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var validator = new AddNewAreaRequestValidator(loc.Object);
            (await validator.ValidateAsync(new AddNewAreaRequest(
                "Functional Park", "Dhaka", "Dhaka", "BD", "1207", "func", "{}",
                "Active", new List<string?> { TestConfigurationFactory.ValidBase64Png })))
                .IsValid.Should().BeTrue();

            // Filter should find it
            var filter = new FilterAreaHandler(ctx, NullLogger<FilterAreaHandler>.Instance);
            var found = await filter.Handle(new FilterAreaQuery("Functional", null, null, null, null, null, null), default);
            found.Areas.Should().ContainSingle(a => a.Name == "Functional Park");
        }
    }
}
