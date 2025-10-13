using Application.Lanes.Services;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.Lanes.Services
{
    public sealed class LaneReadServiceTests
    {
        [Fact]
        public async Task Get_Returns_Entity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new LaneRepository(db);
            var svc = new LaneReadService(repo);

            var (_, lId) = TestDataFactory.SeedProjectWithLane(db);

            var found = await svc.GetAsync(lId);
            found.Should().NotBeNull();
        }

        [Fact]
        public async Task Get_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new LaneRepository(db);
            var svc = new LaneReadService(repo);

            var found = await svc.GetAsync(Guid.Empty);
            found.Should().BeNull();
        }

        [Fact]
        public async Task ListByProject_Returns_Ordered()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new LaneRepository(db);
            var svc = new LaneReadService(repo);

            var firstLaneName = "Lane A";
            var secondLaneName = "Lane B";
            var (pId, _) = TestDataFactory.SeedProjectWithLane(db, laneName: firstLaneName, order: 0);
            TestDataFactory.SeedLane(db, pId, name: secondLaneName, order: 1);

            var list = await svc.ListByProjectAsync(pId);
            list.Select(x => x.Name.Value).Should().ContainInOrder(firstLaneName, secondLaneName);
        }

        [Fact]
        public async Task ListByProject_Returns_Empty_When_None()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new LaneRepository(db);
            var svc = new LaneReadService(repo);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await svc.ListByProjectAsync(pId);
            list.Should().BeEmpty();
        }
    }
}
