using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class LaneRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Persists_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new LaneRepository(db);

            var projectId = TestDataFactory.SeedUserWithProject(db);

            var lane = Lane.Create(projectId, LaneName.Create("Backlog"), 0);
            await repo.AddAsync(lane);
            await repo.SaveChangesAsync();

            var fromDb = await db.Lanes.AsNoTracking().SingleAsync(l => l.Id == lane.Id);
            fromDb.Name.Value.Should().Be("Backlog");
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task RenameAsync_Updates_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, pId, "Todo", 0);

            var res = await repo.RenameAsync(lane.Id, "In Progress", lane.RowVersion ?? Array.Empty<byte>());
            res.Should().Be(DomainMutation.Updated);
            var fromDb = await db.Lanes.AsNoTracking().SingleAsync(l => l.Id == lane.Id);
            fromDb.Name.Value.Should().Be("In Progress");
        }

        [Fact]
        public async Task RenameAsync_NoOp_When_Same_Name()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, pId, "Todo", 1);

            var res = await repo.RenameAsync(lane.Id, "Todo", lane.RowVersion!);
            res.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task RenameAsync_Conflict_When_Duplicate_Name_In_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var pId = TestDataFactory.SeedUserWithProject(db);
            TestDataFactory.SeedLane(db, pId, "Todo", 0);
            var doingLane = TestDataFactory.SeedLane(db, pId, "Doing", 1);

            var res = await repo.RenameAsync(doingLane.Id, "Todo", doingLane.RowVersion!);
            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task ReorderAsync_Reindexes_Without_Gaps()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new LaneRepository(db);

            var pId = TestDataFactory.SeedUserWithProject(db);
            TestDataFactory.SeedLane(db, pId, "Lane A", 0);
            TestDataFactory.SeedLane(db, pId, "Lane B", 1);
            var laneC = TestDataFactory.SeedLane(db, pId, "Lane C", 2);

            // reload tracked 'c' to get current RowVersion
            var trackedC = await db.Lanes.SingleAsync(l => l.Id == laneC.Id);

            var res = await repo.ReorderAsync(trackedC.Id, 0, trackedC.RowVersion!);
            res.Should().Be(DomainMutation.Updated);

            var lanes = await db.Lanes.AsNoTracking()
                .Where(l => l.ProjectId == pId)
                .OrderBy(l => l.Order)
                .ToListAsync();

            lanes.Select(l => l.Name.Value).Should().Equal("Lane C", "Lane A", "Lane B");
            lanes.Select(l => l.Order).Should().Equal(0, 1, 2);
        }

        [Fact]
        public async Task DeleteAsync_Removes_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, pId, "Trash", 0);

            var tracked = await db.Lanes.SingleAsync(l => l.Id == lane.Id);

            var res = await repo.DeleteAsync(tracked.Id, tracked.RowVersion!);
            res.Should().Be(DomainMutation.Deleted);

            var exists = await db.Lanes.AnyAsync(l => l.Id == lane.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_NotFound_When_Unknown_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var res = await repo.DeleteAsync(Guid.NewGuid(), Array.Empty<byte>());
            res.Should().Be(DomainMutation.NotFound);
        }

        [Fact]
        public async Task DeleteAsync_Concurrency_Failure()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var lane = TestDataFactory.SeedLane(db, pId, "Trash", 0);

            var res = await repo.DeleteAsync(lane.Id, Array.Empty<byte>());
            res.Should().Be(DomainMutation.Conflict);

            var stillExists = await db.Lanes.AnyAsync(l => l.Id == lane.Id);
            stillExists.Should().BeTrue();
        }
    }
}
