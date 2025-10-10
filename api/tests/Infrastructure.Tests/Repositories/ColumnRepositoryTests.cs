using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class ColumnRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Persists_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new ColumnRepository(db);

            var projectId = TestDataFactory.SeedUserWithProject(db);
            var laneId = TestDataFactory.SeedLane(db, projectId).Id;
            var columnName = "Column name";

            var column = Column.Create(projectId, laneId, ColumnName.Create(columnName), 0);
            await repo.AddAsync(column);
            await repo.SaveChangesAsync();

            var fromDb = await db.Columns.AsNoTracking().SingleAsync(c => c.Id == column.Id);
            fromDb.Name.Value.Should().Be(columnName);
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task RenameAsync_Updates_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, pId, lId);

            var newName = "new name";

            var res = await repo.RenameAsync(column.Id, newName, column.RowVersion ?? Array.Empty<byte>());
            res.Should().Be(DomainMutation.Updated);
            var fromDb = await db.Columns.AsNoTracking().SingleAsync(c => c.Id == column.Id);
            fromDb.Name.Value.Should().Be(newName);
        }

        [Fact]
        public async Task RenameAsync_NoOp_When_Same_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var sameName = "Same Column Name";
            var column = TestDataFactory.SeedColumn(db, pId, lId, name: sameName);

            var res = await repo.RenameAsync(column.Id, sameName, column.RowVersion!);
            res.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task RenameAsync_Conflict_When_Duplicate_Name_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);

            var sameName = "Same Column Name";
            TestDataFactory.SeedColumn(db, pId, lId, name: sameName);

            var defaultNameColumn = TestDataFactory.SeedColumn(db, pId, lId, order: 1);

            var res = await repo.RenameAsync(defaultNameColumn.Id, sameName, defaultNameColumn.RowVersion!);
            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task ReorderAsync_Reindexes_Without_Gaps()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new ColumnRepository(db);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, pId, lId, "Column A", 0);
            TestDataFactory.SeedColumn(db, pId, lId, "Column B", 1);
            var columnC = TestDataFactory.SeedColumn(db, pId, lId, "Column C", 2);

            // reload tracked 'c' to get current RowVersion
            var trackedC = await db.Columns.SingleAsync(c => c.Id == columnC.Id);

            var res = await repo.ReorderAsync(trackedC.Id, 0, trackedC.RowVersion!);
            res.Should().Be(DomainMutation.Updated);

            var columns = await db.Columns.AsNoTracking()
                .Where(c => c.ProjectId == pId && c.LaneId == lId)
                .OrderBy(c => c.Order)
                .ToListAsync();

            columns.Select(c => c.Name.Value).Should().Equal("Column C", "Column A", "Column B");
            columns.Select(c => c.Order).Should().Equal(0, 1, 2);
        }

        [Fact]
        public async Task DeleteAsync_Removes_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, pId, lId);

            var tracked = await db.Columns.SingleAsync(c => c.Id == column.Id);

            var res = await repo.DeleteAsync(tracked.Id, tracked.RowVersion!);
            res.Should().Be(DomainMutation.Deleted);

            var exists = await db.Columns.AnyAsync(c => c.Id == column.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_NotFound_When_Unknown_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var res = await repo.DeleteAsync(Guid.NewGuid(), Array.Empty<byte>());
            res.Should().Be(DomainMutation.NotFound);
        }

        [Fact]
        public async Task DeleteAsync_Concurrency_Failure()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, pId, lId);

            var res = await repo.DeleteAsync(column.Id, Array.Empty<byte>());
            res.Should().Be(DomainMutation.Conflict);

            var stillExists = await db.Columns.AnyAsync(c => c.Id == column.Id);
            stillExists.Should().BeTrue();
        }
    }
}
