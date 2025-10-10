using Application.Lanes.Abstractions;
using Application.Lanes.Services;
using Domain.Entities;
using Domain.ValueObjects;
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
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneReadService(repo);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var lane = Lane.Create(pId, LaneName.Create("Read"), 0);
            db.Lanes.Add(lane);
            await db.SaveChangesAsync();

            var found = await svc.GetAsync(lane.Id);
            found.Should().NotBeNull();
        }

        [Fact]
        public async Task Get_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneReadService(repo);

            var found = await svc.GetAsync(Guid.Empty);
            found.Should().BeNull();
        }

        [Fact]
        public async Task ListByProject_Returns_Ordered()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneReadService(repo);

            var pId = TestDataFactory.SeedUserWithProject(db);
            db.Lanes.AddRange(
                Lane.Create(pId, LaneName.Create("Lane B"), 1),
                Lane.Create(pId, LaneName.Create("Lane A"), 0));
            await db.SaveChangesAsync();

            var list = await svc.ListByProjectAsync(pId);
            list.Select(x => x.Name.Value).Should().ContainInOrder("Lane A", "Lane B");
        }

        [Fact]
        public async Task ListByProject_Returns_Empty_When_None()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneReadService(repo);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var list = await svc.ListByProjectAsync(pId);
            list.Should().BeEmpty();
        }
    }
}
