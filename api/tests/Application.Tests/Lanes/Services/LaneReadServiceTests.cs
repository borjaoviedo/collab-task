using Application.Common.Exceptions;
using Application.Lanes.Services;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Application.Tests.Lanes.Services
{
    [IntegrationTest]
    public sealed class LaneReadServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_Entity()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var (_, laneId, _) = TestDataFactory.SeedProjectWithLane(db);

            var found = await readSvc.GetByIdAsync(laneId);
            found.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Throws_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            var (_, readSvc) = await CreateSutAsync(dbh);


            await FluentActions.Invoking(() =>
                readSvc.GetByIdAsync(Guid.NewGuid()))
                .Should()
                .ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ListByProjectIdAsync_Returns_Ordered()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var firstLaneName = "Lane A";
            var secondLaneName = "Lane B";

            var (projectId, _, _) = TestDataFactory.SeedProjectWithLane(
                db,
                laneName: firstLaneName,
                order: 0);

            TestDataFactory.SeedLane(db, projectId, secondLaneName, order: 1);

            var list = await readSvc.ListByProjectIdAsync(projectId);

            list.Select(l => l.Name).Should().ContainInOrder(firstLaneName, secondLaneName);
        }

        [Fact]
        public async Task ListByProjectIdAsync_Returns_Empty_When_None()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var list = await readSvc.ListByProjectIdAsync(projectId);
            list.Should().BeEmpty();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, LaneReadService Service)>
            CreateSutAsync(SqliteTestDb dbh)
        {
            var db = dbh.CreateContext();
            var repo = new LaneRepository(db);


            var svc = new LaneReadService(repo);

            return Task.FromResult((db, svc));
        }
    }
}
