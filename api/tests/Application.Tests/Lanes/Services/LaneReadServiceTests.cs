using Application.Lanes.Services;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Application.Tests.Lanes.Services
{
    public sealed class LaneReadServiceTests
    {
        [Fact]
        public async Task Get_Returns_Entity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var readSvc = new LaneReadService(repo);

            var (_, laneId) = TestDataFactory.SeedProjectWithLane(db);

            var found = await readSvc.GetAsync(laneId);
            found.Should().NotBeNull();
        }

        [Fact]
        public async Task Get_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var readSvc = new LaneReadService(repo);

            var found = await readSvc.GetAsync(laneId: Guid.Empty);
            found.Should().BeNull();
        }

        [Fact]
        public async Task ListByProject_Returns_Ordered()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var readSvc = new LaneReadService(repo);

            var firstLaneName = "Lane A";
            var secondLaneName = "Lane B";
            var (projectId, _) = TestDataFactory.SeedProjectWithLane(
                db,
                laneName:
                firstLaneName,
                order: 0);
            TestDataFactory.SeedLane(db, projectId, secondLaneName, order: 1);

            var list = await readSvc.ListByProjectAsync(projectId);
            list.Select(l => l.Name.Value).Should().ContainInOrder(firstLaneName, secondLaneName);
        }

        [Fact]
        public async Task ListByProject_Returns_Empty_When_None()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var readSvc = new LaneReadService(repo);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await readSvc.ListByProjectAsync(projectId);
            list.Should().BeEmpty();
        }
    }
}
