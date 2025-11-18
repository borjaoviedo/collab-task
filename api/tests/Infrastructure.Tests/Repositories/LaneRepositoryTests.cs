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
    public sealed class LaneRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_Lane_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, projectId);

            var existing = await repo.GetByIdAsync(lane.Id);
            existing.Should().NotBeNull();

            var notFound = await repo.GetByIdAsync(laneId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_Returns_Lane_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, projectId);

            var existing = await repo.GetByIdForUpdateAsync(lane.Id);
            existing.Should().NotBeNull();

            var notFound = await repo.GetByIdForUpdateAsync(laneId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ExistsWithNameAsync_Returns_True_When_Exists_Otherwise_False()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, projectId);

            var existing = await repo.ExistsWithNameAsync(projectId, lane.Name);
            existing.Should().BeTrue();

            var notFound = await repo.ExistsWithNameAsync(projectId, LaneName.Create("diff"));
            notFound.Should().BeFalse();
        }

        [Fact]
        public async Task GetMaxOrderAsync_Returns_MaxOrder()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _, _) = TestDataFactory.SeedProjectWithLane(db);

            var maxOrder = await repo.GetMaxOrderAsync(projectId);
            maxOrder.Should().Be(0);

            TestDataFactory.SeedLane(db, projectId, order: 1);
            maxOrder = await repo.GetMaxOrderAsync(projectId);
            maxOrder.Should().Be(1);

            TestDataFactory.SeedLane(db, projectId, order: 7);
            maxOrder = await repo.GetMaxOrderAsync(projectId);
            maxOrder.Should().Be(7);

            TestDataFactory.SeedLane(db, projectId, order: 3);
            maxOrder = await repo.GetMaxOrderAsync(projectId);
            maxOrder.Should().Be(7);
        }

        [Fact]
        public async Task ListByProjectIdAsync_Returns_List_When_Columns_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _, _) = TestDataFactory.SeedProjectWithLane(db);

            var list = await repo.ListByProjectIdAsync(projectId);
            list.Should().HaveCount(1);

            TestDataFactory.SeedLane(db, projectId, order: 1);
            list = await repo.ListByProjectIdAsync(projectId);
            list.Should().HaveCount(2);

            TestDataFactory.SeedLane(db, projectId, order: 2);
            list = await repo.ListByProjectIdAsync(projectId);
            list.Should().HaveCount(3);
        }

        [Fact]
        public async Task ListByProjectIdAsync_Returns_Empty_List_When_No_Column_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await repo.ListByProjectIdAsync(projectId);
            list.Should().BeEmpty();
        }

        // --------------- Add / Update / Remove ---------------

        [Fact]
        public async Task AddAsync_Persists_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);
            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var laneName = "lane name";
            var lane = Lane.Create(projectId, LaneName.Create(laneName), order: 0);

            await repo.AddAsync(lane);
            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await db.Lanes
                .AsNoTracking()
                .SingleAsync(l => l.Id == lane.Id);
            fromDb.Name.Value.Should().Be(laneName);
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task UpdateAsync_Marks_Entity_Modified_And_Persists_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (_, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var lane = await repo.GetByIdForUpdateAsync(laneId);

            // Modify through domain behavior
            lane!.Rename(LaneName.Create("Updated Name"));

            await repo.UpdateAsync(lane);
            await db.SaveChangesAsync();

            var reloaded = await db.Lanes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == laneId);

            reloaded.Should().NotBeNull();
            reloaded!.Name.Value.Should().Be("Updated Name");
        }

        [Fact]
        public async Task RemoveAsync_Deletes_User_On_SaveChanges()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (_, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var lane = await repo.GetByIdForUpdateAsync(laneId);

            await repo.RemoveAsync(lane!);
            await db.SaveChangesAsync();

            var reloaded = await db.Lanes
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == laneId);

            reloaded.Should().BeNull();
        }
    }
}
