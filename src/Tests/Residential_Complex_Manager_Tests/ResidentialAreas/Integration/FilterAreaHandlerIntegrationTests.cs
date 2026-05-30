using Microsoft.Extensions.Logging.Abstractions;
using ResidentialAreas.API.Enum;
using ResidentialAreas.API.EntityModels;
using ResidentialAreas.API.ResidentiaAreas.Areas.FilterArea;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Integration
{
    public class FilterAreaHandlerIntegrationTests
    {
        private static Area Make(string name, string city, string state, Status status, long code)
            => new()
            {
                Id = Guid.NewGuid(), Code = code, Name = name, City = city, State = state,
                Country = "BD", PostalCode = "1207", Address = "addr",
                GeoBoundary = "{}", Status = status,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };

        [Fact]
        public async Task Returns_all_areas_when_no_filter_is_supplied()
        {
            await using var scope = new SqliteAreaDbContextScope();
            var ctx = scope.Context;
            ctx.Areas.AddRange(
                Make("North Park", "Dhaka", "Dhaka", Status.Active,       1_000_000_001),
                Make("South Park", "Khulna", "Khulna", Status.Inactive,   1_000_000_002));
            await ctx.SaveChangesAsync();

            var sut = new FilterAreaHandler(ctx, NullLogger<FilterAreaHandler>.Instance);
            var result = await sut.Handle(new FilterAreaQuery(null, null, null, null, null, null, null), default);

            result.ErrorMessage.Should().BeNull();
            result.Areas.Should().HaveCount(2);
        }

        [Fact]
        public async Task Applies_substring_filter_on_name()
        {
            await using var scope = new SqliteAreaDbContextScope();
            var ctx = scope.Context;
            ctx.Areas.AddRange(
                Make("North Park", "Dhaka", "Dhaka", Status.Active, 1_000_000_001),
                Make("South Garden", "Khulna", "Khulna", Status.Active, 1_000_000_002));
            await ctx.SaveChangesAsync();

            var sut = new FilterAreaHandler(ctx, NullLogger<FilterAreaHandler>.Instance);
            var result = await sut.Handle(new FilterAreaQuery("Park", null, null, null, null, null, null), default);

            result.Areas.Should().ContainSingle();
        }

        [Fact]
        public async Task Status_filter_is_case_insensitive()
        {
            await using var scope = new SqliteAreaDbContextScope();
            var ctx = scope.Context;
            ctx.Areas.Add(Make("X", "C", "S", Status.Inactive, 1_000_000_001));
            await ctx.SaveChangesAsync();

            var sut = new FilterAreaHandler(ctx, NullLogger<FilterAreaHandler>.Instance);
            var result = await sut.Handle(new FilterAreaQuery(null, null, null, null, null, null, "inactive"), default);
            result.Areas.Should().ContainSingle();
        }

        [Fact]
        public async Task Handle_returns_an_error_result_when_status_string_is_not_a_valid_enum()
        {
            await using var scope = new SqliteAreaDbContextScope();
            var ctx = scope.Context;
            var sut = new FilterAreaHandler(ctx, NullLogger<FilterAreaHandler>.Instance);

            var result = await sut.Handle(
                new FilterAreaQuery(null, null, null, null, null, null, "BogusEnumValue"), default);

            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.Areas.Should().BeNull();
        }
    }
}
