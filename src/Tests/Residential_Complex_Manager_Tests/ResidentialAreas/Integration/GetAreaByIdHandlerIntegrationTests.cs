using Microsoft.Extensions.Logging.Abstractions;
using ResidentialAreas.API.Enum;
using ResidentialAreas.API.EntityModels;
using ResidentialAreas.API.ResidentiaAreas.Areas.GetAreaById;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Integration
{
    public class GetAreaByIdHandlerIntegrationTests
    {
        [Fact]
        public async Task Returns_area_when_present()
        {
            await using var scope = new SqliteAreaDbContextScope();
            var ctx = scope.Context;
            var id = Guid.NewGuid();
            ctx.Areas.Add(new Area
            {
                Id = id, Code = 1_000_000_001, Name = "X", City = "C", State = "S",
                Country = "BD", PostalCode = "1207", Address = "addr",
                GeoBoundary = "{}", Status = Status.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var sut = new GetAreaByIdHandler(ctx, NullLogger<GetAreaByIdHandler>.Instance);
            var result = await sut.Handle(new GetAreaByIdQuery(id), default);
            result.Should().NotBeNull();
            result!.Id.Should().Be(id);
            result.Name.Should().Be("X");
        }

        [Fact]
        public async Task Handle_honours_its_non_nullable_return_contract_when_area_is_missing()
        {
            await using var scope = new SqliteAreaDbContextScope();
            var ctx = scope.Context;
            var sut = new GetAreaByIdHandler(ctx, NullLogger<GetAreaByIdHandler>.Instance);

            var result = await sut.Handle(new GetAreaByIdQuery(Guid.NewGuid()), default);

            result.Should().NotBeNull();
        }
    }
}
