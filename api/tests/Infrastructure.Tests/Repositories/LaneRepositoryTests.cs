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
        public async Task GetTrackedByIdAsync_Returns_Lane_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, projectId);

            var existing = await repo.GetTrackedByIdAsync(lane.Id);
            existing.Should().NotBeNull();

            var notFound = await repo.GetTrackedByIdAsync(laneId: Guid.NewGuid());
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

            var (projectId, _) = TestDataFactory.SeedProjectWithLane(db);

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
        public async Task ListByProjectAsync_Returns_List_When_Columns_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _) = TestDataFactory.SeedProjectWithLane(db);

            var list = await repo.ListByProjectAsync(projectId);
            list.Should().HaveCount(1);

            TestDataFactory.SeedLane(db, projectId, order: 1);
            list = await repo.ListByProjectAsync(projectId);
            list.Should().HaveCount(2);

            TestDataFactory.SeedLane(db, projectId, order: 2);
            list = await repo.ListByProjectAsync(projectId);
            list.Should().HaveCount(3);
        }

        [Fact]
        public async Task ListByProjectAsync_Returns_Empty_List_When_No_Column_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await repo.ListByProjectAsync(projectId);
            list.Should().BeEmpty();
        }

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
        public async Task RenameAsync_Updates_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, projectId);

            var laneName = "lane name";
            var rowVersion = lane.RowVersion ?? [];

            var result = await repo.RenameAsync(lane.Id, LaneName.Create(laneName), rowVersion);
            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Update);

            var fromDb = await db.Lanes.AsNoTracking().SingleAsync(l => l.Id == lane.Id);
            fromDb.Name.Value.Should().Be(laneName);
        }

        [Fact]
        public async Task RenameAsync_NoOp_When_Same_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var sameName = LaneName.Create("Todo");
            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, projectId, sameName, order: 1);

            var result = await repo.RenameAsync(lane.Id, sameName, lane.RowVersion);
            result.Should().Be(PrecheckStatus.NoOp);
        }

        [Fact]
        public async Task RenameAsync_Returns_Conflict_When_Duplicate_Name_In_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var sameName = LaneName.Create("Todo");
            var (projectId, _) = TestDataFactory.SeedProjectWithLane(db, laneName: sameName);
            var doingLane = TestDataFactory.SeedLane(db, projectId, "Doing", order: 1);

            var result = await repo.RenameAsync(doingLane.Id, sameName, doingLane.RowVersion);
            result.Should().Be(PrecheckStatus.Conflict);
        }

        [Fact]
        public async Task ReorderAsync_Reindexes_Without_Gaps()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);

            var firstLaneName = "Lane A";
            var secondLaneName = "Lane B";
            var thirdLaneName = "Lane C";
            var (projectId, _) = TestDataFactory.SeedProjectWithLane(
                db,
                laneName: firstLaneName,
                order: 0);
            TestDataFactory.SeedLane(
                db,
                projectId,
                secondLaneName,
                order: 1);
            var laneC = TestDataFactory.SeedLane(
                db,
                projectId,
                thirdLaneName,
                order: 2);

            // Reload tracked 'c' to get current RowVersion
            var trackedC = await db.Lanes.SingleAsync(l => l.Id == laneC.Id);

            await repo.ReorderPhase1Async(trackedC.Id, newOrder: 0, trackedC.RowVersion);
            await uow.SaveAsync(MutationKind.Update);

            await repo.ApplyReorderPhase2Async(trackedC.Id);
            await uow.SaveAsync(MutationKind.Update);

            var lanes = await db.Lanes
                                .AsNoTracking()
                                .Where(l => l.ProjectId == projectId)
                                .OrderBy(l => l.Order)
                                .ToListAsync();

            lanes.Select(l => l.Name.Value).Should().Equal(thirdLaneName, firstLaneName, secondLaneName);
            lanes.Select(l => l.Order).Should().Equal(0, 1, 2);
        }

        [Fact]
        public async Task DeleteAsync_Removes_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);

            var (_, laneId) = TestDataFactory.SeedProjectWithLane(db);

            var tracked = await db.Lanes.SingleAsync(l => l.Id == laneId);

            var result = await repo.DeleteAsync(tracked.Id, tracked.RowVersion);
            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Delete);

            var exists = await db.Lanes.AnyAsync(l => l.Id == laneId);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_NotFound_When_Unknown_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var result = await repo.DeleteAsync(laneId: Guid.NewGuid(), rowVersion: []);
            result.Should().Be(PrecheckStatus.NotFound);
        }
    }
}
