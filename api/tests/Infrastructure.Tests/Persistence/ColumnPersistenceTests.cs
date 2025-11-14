using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Persistence
{
    [Collection("SqlServerContainer")]
    public sealed class ColumnPersistenceTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_Get_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var column = Column.Create(
                projectId,
                laneId,
                ColumnName.Create("To Do"),
                order: 0);
            db.Columns.Add(column);
            await db.SaveChangesAsync();

            var fromDb = await db.Columns
                .AsNoTracking()
                .SingleAsync(c => c.Id == column.Id);
            fromDb.Name.Value.Should().Be("To Do");
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task Unique_Index_LaneId_Name_Is_Enforced()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var name = ColumnName.Create("Same");

            db.Columns.Add(Column.Create(projectId, laneId, name, order: 0));
            await db.SaveChangesAsync();

            var dup = Column.Create(projectId, laneId, name, order: 1);
            db.Columns.Add(dup);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.Entry(dup).State = EntityState.Detached;

            // Same column name in other lane is allowed
            var lane2 = TestDataFactory.SeedLane(db, projectId, order: 1);
            db.Columns.Add(Column.Create(projectId, lane2.Id, name, order: 0));
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Rename()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(
                db,
                projectId,
                laneId,
                "column A",
                order: 0);

            var stale = column.RowVersion.ToArray();

            column.Rename(ColumnName.Create("column B"));
            db.Entry(column).Property(x => x.Name).IsModified = true;
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<CollabTaskDbContext>();
            var same = await db2.Columns.SingleAsync(x => x.Id == column.Id);

            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Rename(ColumnName.Create("column C"));
            db2.Entry(same).Property(x => x.Name).IsModified = true;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }
    }
}
