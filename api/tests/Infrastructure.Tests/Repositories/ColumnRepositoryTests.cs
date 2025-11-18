using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Repositories
{
    [IntegrationTest]
    public sealed class ColumnRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_Column_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var existing = await repo.GetByIdAsync(column.Id);
            existing.Should().NotBeNull();

            var notFound = await repo.GetByIdAsync(columnId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_Returns_Column_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var existing = await repo.GetByIdForUpdateAsync(column.Id);
            existing.Should().NotBeNull();

            var notFound = await repo.GetByIdForUpdateAsync(columnId: Guid.NewGuid());
            notFound.Should().Be(null);
        }

        [Fact]
        public async Task ExistsWithNameAsync_Returns_True_When_Exists_Otherwise_False()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
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

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
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
        public async Task ListByLaneIdAsync_Returns_List_When_Columns_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, projectId, laneId, order: 0);
            var list = await repo.ListByLaneIdAsync(laneId);
            list.Should().HaveCount(1);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 1);
            list = await repo.ListByLaneIdAsync(laneId);
            list.Should().HaveCount(2);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 2);
            list = await repo.ListByLaneIdAsync(laneId);
            list.Should().HaveCount(3);
        }

        [Fact]
        public async Task ListByLaneAsync_Returns_Empty_List_When_No_Column_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (_, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var list = await repo.ListByLaneIdAsync(laneId);

            list.Should().BeEmpty();
        }

        // --------------- Add / Update / Remove ---------------

        [Fact]
        public async Task AddAsync_Persists_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
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
        public async Task UpdateAsync_Marks_Entity_Modified_And_Persists_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (_, _, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var column = await repo.GetByIdForUpdateAsync(columnId);

            // Modify through domain behavior
            column!.Rename(ColumnName.Create("Updated Name"));

            await repo.UpdateAsync(column);
            await db.SaveChangesAsync();

            var reloaded = await db.Columns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == columnId);

            reloaded.Should().NotBeNull();
            reloaded!.Name.Value.Should().Be("Updated Name");
        }

        [Fact]
        public async Task RemoveAsync_Deletes_User_On_SaveChanges()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var (_, _, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var column = await repo.GetByIdForUpdateAsync(columnId);

            await repo.RemoveAsync(column!);
            await db.SaveChangesAsync();

            var reloaded = await db.Columns
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == columnId);

            reloaded.Should().BeNull();
        }
    }
}
