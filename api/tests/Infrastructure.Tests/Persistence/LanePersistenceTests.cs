using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Tests.Containers;
using TestHelpers.Persistence;
using TestHelpers.Common;

namespace Infrastructure.Tests.Persistence
{
    [Collection("SqlServerContainer")]
    public sealed class LanePersistenceTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_Get_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var laneName = "Backlog";
            var lane = Lane.Create(projectId, LaneName.Create(laneName), order: 0);
            db.Lanes.Add(lane);
            await db.SaveChangesAsync();

            var fromDb = await db.Lanes
                .AsNoTracking()
                .SingleAsync(l => l.Id == lane.Id);
            fromDb.Name.Value.Should().Be(laneName);
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task Unique_Index_ProjectId_Name_Is_Enforced()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var laneName = "Todo";
            var (projectId, _, _) = TestDataFactory.SeedProjectWithLane(db, laneName: laneName);
            var dup = Lane.Create(projectId, LaneName.Create(laneName), 1);
            db.Lanes.Add(dup);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

            db.Entry(dup).State = EntityState.Detached;

            // Same name in different project allowed
            var (project2, _) = TestDataFactory.SeedUserWithProject(db);
            db.Lanes.Add(Lane.Create(project2, LaneName.Create(laneName), order: 0));
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Rename()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, projectId, "Lane A", order: 0);

            var stale = lane.RowVersion.ToArray();

            lane.Rename(LaneName.Create("Lane B"));
            db.Entry(lane).Property(x => x.Name).IsModified = true;
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.Lanes.SingleAsync(x => x.Id == lane.Id);

            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Rename(LaneName.Create("Lane C"));
            db2.Entry(same).Property(x => x.Name).IsModified = true;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }
    }
}
