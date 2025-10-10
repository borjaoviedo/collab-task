using Application.Lanes.Abstractions;
using Application.Lanes.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Application.Tests.Lanes.Services
{
    public sealed class LaneWriteServiceTests
    {
        [Fact]
        public async Task Create_Assigns_Next_Order_When_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneWriteService(repo);

            var pId = TestDataFactory.SeedUserWithProject(db);
            await svc.CreateAsync(pId, LaneName.Create("First"), null);
            var (created, lane) = await svc.CreateAsync(pId, LaneName.Create("Second"), null);

            created.Should().Be(DomainMutation.Created);
            lane!.Order.Should().Be(1);
        }

        [Fact]
        public async Task Create_Returns_Conflict_On_Duplicate_Name_In_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneWriteService(repo);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var (m1, _) = await svc.CreateAsync(pId, "Dup");
            m1.Should().Be(DomainMutation.Created);

            var (m2, lane2) = await svc.CreateAsync(pId, "Dup");
            m2.Should().Be(DomainMutation.Conflict);
            lane2.Should().BeNull();
        }

        [Fact]
        public async Task Rename_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneWriteService(repo);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var (_, lane) = await svc.CreateAsync(pId, LaneName.Create("Old"), 0);

            var current = await db.Lanes.FirstAsync(x => x.Id == lane!.Id);
            var result = await svc.RenameAsync(lane!.Id, LaneName.Create("New"), current.RowVersion);
            result.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task Reorder_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneWriteService(repo);

            var pId = TestDataFactory.SeedUserWithProject(db);
            await svc.CreateAsync(pId, "Lane A"); // order 0
            await svc.CreateAsync(pId, "Lane B"); // order 1
            var (_, laneC) = await svc.CreateAsync(pId, "Lane C"); // order 2

            var current = await db.Lanes.FirstAsync(x => x.Id == laneC!.Id);
            var result = await svc.ReorderAsync(laneC!.Id, 1, current.RowVersion!);
            result.Should().Be(DomainMutation.Updated);

            var names = await db.Lanes.AsNoTracking()
                .Where(l => l.ProjectId == pId)
                .OrderBy(l => l.Order)
                .Select(l => l.Name.Value)
                .ToListAsync();

            names.Should().Equal("Lane A", "Lane C", "Lane B");
        }

        [Fact]
        public async Task Delete_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            ILaneRepository repo = new LaneRepository(db);
            var svc = new LaneWriteService(repo);

            var pId = TestDataFactory.SeedUserWithProject(db);
            var (_, lane) = await svc.CreateAsync(pId, LaneName.Create("Temp"), 0);

            var current = await db.Lanes.FirstAsync(x => x.Id == lane!.Id);
            var del = await svc.DeleteAsync(lane!.Id, current.RowVersion);
            del.Should().Be(DomainMutation.Deleted);
        }
    }
}
