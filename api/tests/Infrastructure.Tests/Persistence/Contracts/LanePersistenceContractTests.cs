using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers;
using Infrastructure.Tests.Containers;

namespace Infrastructure.Tests.Persistence.Contracts
{
    [Collection("SqlServerContainer")]
    public sealed class LanePersistenceContractTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_Get_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var l = Lane.Create(pId, LaneName.Create("Backlog"), 0);
            db.Lanes.Add(l);
            await db.SaveChangesAsync();

            var fromDb = await db.Lanes.AsNoTracking().SingleAsync(x => x.Id == l.Id);
            fromDb.Name.Value.Should().Be("Backlog");
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task Unique_Index_ProjectId_Name_Is_Enforced()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (pId, _) = TestDataFactory.SeedProjectWithLane(db, laneName: "Todo");
            var dup = Lane.Create(pId, LaneName.Create("Todo"), 1);
            db.Lanes.Add(dup);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

            db.Entry(dup).State = EntityState.Detached;

            // same name in different project allowed
            var (p2, _) = TestDataFactory.SeedUserWithProject(db);
            db.Lanes.Add(Lane.Create(p2, LaneName.Create("Todo"), 0));
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Rename()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var l = TestDataFactory.SeedLane(db, pId, "Lane A", 0);

            var stale = l.RowVersion!.ToArray();

            l.Rename(LaneName.Create("Lane B"));
            db.Entry(l).Property(x => x.Name).IsModified = true;
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.Lanes.SingleAsync(x => x.Id == l.Id);
            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Rename(LaneName.Create("Lane C"));
            db2.Entry(same).Property(x => x.Name).IsModified = true;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }
    }
}
