using Microsoft.Extensions.Logging.Abstractions;
using NBomber.CSharp;
using ResidentialAreas.API.Enum;
using ResidentialAreas.API.EntityModels;
using ResidentialAreas.API.ResidentiaAreas.Areas.FilterArea;
using Residential_Complex_Manager_Tests.Common;

namespace Residential_Complex_Manager_Tests.ResidentialAreas.Load
{
    public class AreaLoadTests
    {
        [Fact]
        public async Task FilterAreaHandler_sustains_load_over_a_seeded_dataset()
        {
            await using var scope = new SqliteAreaDbContextScope();
            var ctx = scope.Context;
            for (int i = 0; i < 200; i++)
            {
                ctx.Areas.Add(new Area
                {
                    Id = Guid.NewGuid(), Code = 1_000_000_000 + i,
                    Name = $"Area-{i}", City = "Dhaka", State = "Dhaka", Country = "BD",
                    PostalCode = "1207", Address = "addr", GeoBoundary = "{}",
                    Status = Status.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                });
            }
            await ctx.SaveChangesAsync();

            var sut = new FilterAreaHandler(ctx, NullLogger<FilterAreaHandler>.Instance);
            var dbGate = new SemaphoreSlim(1, 1);

            var scenario = Scenario.Create("filter_area_load", async _ =>
            {
                await dbGate.WaitAsync();
                try
                {
                    var r = await sut.Handle(new FilterAreaQuery("Area-", null, null, null, null, null, null), default);
                    return r.ErrorMessage is null ? Response.Ok() : Response.Fail();
                }
                catch (Exception ex) { return Response.Fail(message: ex.Message); }
                finally { dbGate.Release(); }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(Simulation.KeepConstant(copies: 10, during: TimeSpan.FromSeconds(15)));

            var stats = NBomberRunner.RegisterScenarios(scenario)
                                     .WithReportFolder("nbomber-reports/area-load")
                                     .Run();

            stats.AllOkCount.Should().BeGreaterThan(0);
            stats.AllFailCount.Should().Be(0);
        }
    }
}
