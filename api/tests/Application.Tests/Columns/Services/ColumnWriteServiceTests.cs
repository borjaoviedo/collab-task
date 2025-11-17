using Application.Columns.DTOs;
using Application.Columns.Services;
using Application.Common.Exceptions;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Application.Tests.Columns.Services
{
    public sealed class ColumnWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Persists_Column()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc) = await CreateSutAsync(dbh);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var laneId = TestDataFactory.SeedLane(db, projectId).Id;
            var columnName = "Column name";

            var dto = new ColumnCreateDto { Name = columnName, Order = 0 };

            var result = await writeSvc.CreateAsync(projectId, laneId, dto);

            // Assert DTO
            result.Should().NotBeNull();
            result.Name.Should().Be(columnName);
            result.Order.Should().Be(0);

            // Assert DB
            var fromDb = await db.Columns
                .AsNoTracking()
                .SingleAsync(c => c.Id == result.Id);

            fromDb.Name.Value.Should().Be(columnName);
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task CreateAsync_Throws_Conflict_When_Name_Already_Exists_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            var (_, writeSvc) = await CreateSutAsync(dbh);

            var db = dbh.CreateContext();
            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var laneId = TestDataFactory.SeedLane(db, projectId).Id;

            var name = "Duplicate Column";

            // Existing column with same name
            TestDataFactory.SeedColumn(db, projectId, laneId, name);

            var dto = new ColumnCreateDto { Name = name, Order = 1 };

            var act = async () => await writeSvc.CreateAsync(projectId, laneId, dto);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task RenameAsync_Updates_Name()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc) = await CreateSutAsync(dbh);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var newName = "new name";
            var dto = new ColumnRenameDto { NewName = newName };

            var result = await writeSvc.RenameAsync(column.Id, dto);

            result.Should().NotBeNull();
            result.Id.Should().Be(column.Id);
            result.Name.Should().Be(newName);

            var fromDb = await db.Columns
                .AsNoTracking()
                .SingleAsync(c => c.Id == column.Id);

            fromDb.Name.Value.Should().Be(newName);
        }

        [Fact]
        public async Task RenameAsync_Throws_NotFound_When_Column_Does_Not_Exist()
        {
            using var dbh = new SqliteTestDb();
            var (_, writeSvc) = await CreateSutAsync(dbh);

            var dto = new ColumnRenameDto { NewName = "whatever" };
            var act = async () => await writeSvc.RenameAsync(Guid.NewGuid(), dto);

            await act.Should().ThrowAsync<NotFoundException>();
        }


        [Fact]
        public async Task ReorderAsync_Reindexes_Without_Gaps()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc) = await CreateSutAsync(dbh);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, projectId, laneId, "Column A", order: 0);
            TestDataFactory.SeedColumn(db, projectId, laneId, "Column B", order: 1);
            var columnC = TestDataFactory.SeedColumn(db, projectId, laneId, "Column C", order: 2);

            var dto = new ColumnReorderDto { NewOrder = 0 };

            var result = await writeSvc.ReorderAsync(columnC.Id, dto);
            result.Should().NotBeNull();
            result.Id.Should().Be(columnC.Id);
            result.Order.Should().Be(0);

            var columns = await db.Columns.AsNoTracking()
                .Where(c => c.ProjectId == projectId && c.LaneId == laneId)
                .OrderBy(c => c.Order)
                .ToListAsync();

            columns.Select(c => c.Name.Value).Should().Equal("Column C", "Column A", "Column B");
            columns.Select(c => c.Order).Should().Equal(0, 1, 2);
        }

        [Fact]
        public async Task ReorderAsync_Returns_Current_State_When_NoOp()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc) = await CreateSutAsync(dbh);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId, "Column A", order: 0);

            var dto = new ColumnReorderDto { NewOrder = 0 };

            var result = await writeSvc.ReorderAsync(column.Id, dto);

            result.Should().NotBeNull();
            result.Id.Should().Be(column.Id);
            result.Order.Should().Be(0);

            var fromDb = await db.Columns
                .AsNoTracking()
                .SingleAsync(c => c.Id == column.Id);

            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task DeleteByIdAsync_Removes_Column()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc) = await CreateSutAsync(dbh);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            await writeSvc.DeleteByIdAsync(column.Id);

            var exists = await db.Columns.AnyAsync(c => c.Id == column.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteByIdAsync_Throws_NotFound_When_Column_Does_Not_Exist()
        {
            using var dbh = new SqliteTestDb();
            var (_, writeSvc) = await CreateSutAsync(dbh);

            var act = async () => await writeSvc.DeleteByIdAsync(Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, ColumnWriteService Service)>
            CreateSutAsync(SqliteTestDb dbh)
        {
            var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);

            var svc = new ColumnWriteService(repo, uow);

            return Task.FromResult((db, svc));
        }
    }
}
