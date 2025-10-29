using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class ColumnRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_Column_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var existing = await repo.GetByIdAsync(column.Id);
            existing.Should().NotBeNull();

            var notFound = await repo.GetByIdAsync(columnId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task GetTrackedByIdAsync_Returns_Column_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var existing = await repo.GetTrackedByIdAsync(column.Id);
            existing.Should().NotBeNull();

            var notFound = await repo.GetTrackedByIdAsync(columnId: Guid.NewGuid());
            notFound.Should().Be(null);
        }

        [Fact]
        public async Task ExistsWithNameAsync_Returns_True_When_Exists_Otherwise_False()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var existing = await repo.ExistsWithNameAsync(laneId, column.Name);
            existing.Should().BeTrue();

            var notFound = await repo.ExistsWithNameAsync(laneId, ColumnName.Create("other name"));
            notFound.Should().BeFalse();
        }

        [Fact]
        public async Task GetMaxOrderAsync_Returns_MaxOrder()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, projectId, laneId, order: 0);

            var maxOrder = await repo.GetMaxOrderAsync(laneId);
            maxOrder.Should().Be(0);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 1);
            maxOrder = await repo.GetMaxOrderAsync(laneId);
            maxOrder.Should().Be(1);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 7);
            maxOrder = await repo.GetMaxOrderAsync(laneId);
            maxOrder.Should().Be(7);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 3);
            maxOrder = await repo.GetMaxOrderAsync(laneId);
            maxOrder.Should().Be(7);
        }

        [Fact]
        public async Task ListByLaneAsync_Returns_List_When_Columns_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, projectId, laneId, order: 0);
            var list = await repo.ListByLaneAsync(laneId);
            list.Should().HaveCount(1);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 1);
            list = await repo.ListByLaneAsync(laneId);
            list.Should().HaveCount(2);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 2);
            list = await repo.ListByLaneAsync(laneId);
            list.Should().HaveCount(3);
        }

        [Fact]
        public async Task ListByLaneAsync_Returns_Empty_List_When_No_Column_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (_, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var list = await repo.ListByLaneAsync(laneId);

            list.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_Persists_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var laneId = TestDataFactory.SeedLane(db, projectId).Id;
            var columnName = "Column name";

            var column = Column.Create(
                projectId,
                laneId,
                ColumnName.Create(columnName),
                order: 0);
            await repo.AddAsync(column);
            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await db.Columns
                .AsNoTracking()
                .SingleAsync(c => c.Id == column.Id);
            fromDb.Name.Value.Should().Be(columnName);
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task RenameAsync_Updates_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var newName = "new name";
            var rowVersion = column.RowVersion ?? [];
            var result = await repo.RenameAsync(
                column.Id,
                ColumnName.Create(newName),
                rowVersion);

            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Update);

            var fromDb = await db.Columns
                .AsNoTracking()
                .SingleAsync(c => c.Id == column.Id);
            fromDb.Name.Value.Should().Be(newName);
        }

        [Fact]
        public async Task RenameAsync_NoOp_When_Same_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var sameName = "Same Column Name";
            var column = TestDataFactory.SeedColumn(db, projectId, laneId, sameName);

            var result = await repo.RenameAsync(
                column.Id,
                ColumnName.Create(sameName),
                column.RowVersion);
            result.Should().Be(PrecheckStatus.NoOp);
        }

        [Fact]
        public async Task RenameAsync_Conflict_When_Duplicate_Name_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);

            var sameName = "Same Column Name";
            TestDataFactory.SeedColumn(db, projectId, laneId, sameName);

            var defaultNameColumn = TestDataFactory.SeedColumn(
                db,
                projectId,
                laneId,
                order: 1);
            var result = await repo.RenameAsync(
                defaultNameColumn.Id,
                ColumnName.Create(sameName),
                defaultNameColumn.RowVersion);

            result.Should().Be(PrecheckStatus.Conflict);
        }

        [Fact]
        public async Task ReorderAsync_Reindexes_Without_Gaps()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(
                db,
                projectId,
                laneId,
                name: "Column A",
                order: 0);
            TestDataFactory.SeedColumn(
                db,
                projectId,
                laneId,
                name: "Column B",
                order: 1);
            var columnC = TestDataFactory.SeedColumn(
                db,
                projectId,
                laneId,
                name: "Column C",
                order: 2);

            // Reload tracked 'c' to get current RowVersion
            var trackedC = await db.Columns.SingleAsync(c => c.Id == columnC.Id);

            await repo.ReorderPhase1Async(trackedC.Id, newOrder: 0, trackedC.RowVersion);
            await uow.SaveAsync(MutationKind.Update);

            await repo.ApplyReorderPhase2Async(trackedC.Id);
            await uow.SaveAsync(MutationKind.Update);

            var columns = await db.Columns
                .AsNoTracking()
                .Where(c => c.ProjectId == projectId && c.LaneId == laneId)
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
            var uow = new UnitOfWork(db);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var tracked = await db.Columns.SingleAsync(c => c.Id == column.Id);

            var result = await repo.DeleteAsync(tracked.Id, tracked.RowVersion);
            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Delete);

            var exists = await db.Columns.AnyAsync(c => c.Id == column.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_NotFound_When_Unknown_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var result = await repo.DeleteAsync(columnId: Guid.NewGuid(), rowVersion: []);
            result.Should().Be(PrecheckStatus.NotFound);
        }
    }
}
