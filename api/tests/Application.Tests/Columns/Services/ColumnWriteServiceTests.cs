using Application.Columns.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Application.Tests.Columns.Services
{
    public sealed class ColumnWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Persists_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);
            var svc = new ColumnWriteService(repo, uow);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var laneId = TestDataFactory.SeedLane(db, projectId).Id;
            var columnName = "Column name";

            await svc.CreateAsync(projectId, laneId, ColumnName.Create(columnName));
            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await db.Columns.AsNoTracking().SingleAsync(c => c.Name == columnName);
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
            var svc = new ColumnWriteService(repo, uow);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, pId, lId);

            var newName = "new name";

            var res = await svc.RenameAsync(column.Id, ColumnName.Create(newName), column.RowVersion ?? []);
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
            var uow = new UnitOfWork(db);
            var svc = new ColumnWriteService(repo, uow);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var sameName = "Same Column Name";
            var column = TestDataFactory.SeedColumn(db, pId, lId, sameName);

            var res = await svc.RenameAsync(column.Id, ColumnName.Create(sameName), column.RowVersion!);
            res.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task RenameAsync_Conflict_When_Duplicate_Name_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);
            var svc = new ColumnWriteService(repo, uow);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);

            var sameName = "Same Column Name";
            TestDataFactory.SeedColumn(db, pId, lId, name: sameName);

            var defaultNameColumn = TestDataFactory.SeedColumn(db, pId, lId, order: 1);

            var res = await svc.RenameAsync(defaultNameColumn.Id, ColumnName.Create(sameName), defaultNameColumn.RowVersion!);
            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task ReorderAsync_Reindexes_Without_Gaps()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);
            var svc = new ColumnWriteService(repo, uow);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, pId, lId, "Column A", 0);
            TestDataFactory.SeedColumn(db, pId, lId, "Column B", 1);
            var columnC = TestDataFactory.SeedColumn(db, pId, lId, "Column C", 2);

            // reload tracked 'c' to get current RowVersion
            var trackedC = await db.Columns.SingleAsync(c => c.Id == columnC.Id);

            var res = await svc.ReorderAsync(trackedC.Id, 0, trackedC.RowVersion!);
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
            var uow = new UnitOfWork(db);
            var svc = new ColumnWriteService(repo, uow);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, pId, lId);

            var tracked = await db.Columns.SingleAsync(c => c.Id == column.Id);

            var res = await svc.DeleteAsync(tracked.Id, tracked.RowVersion!);
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
            var uow = new UnitOfWork(db);
            var svc = new ColumnWriteService(repo, uow);

            var res = await svc.DeleteAsync(Guid.NewGuid(), []);
            res.Should().Be(DomainMutation.NotFound);
        }

        [Fact]
        public async Task DeleteAsync_Concurrency_Failure()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);
            var svc = new ColumnWriteService(repo, uow);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, pId, lId);

            var res = await svc.DeleteAsync(column.Id, []);
            res.Should().Be(DomainMutation.Conflict);

            var stillExists = await db.Columns.AnyAsync(c => c.Id == column.Id);
            stillExists.Should().BeTrue();
        }
    }
}
