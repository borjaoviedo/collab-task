using Application.Lanes.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Application.Tests.Lanes.Services
{
    public sealed class LaneWriteServiceTests
    {
        [Fact]
        public async Task Create_Assigns_Next_Order_When_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new LaneWriteService(repo, uow);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            await writeSvc.CreateAsync(projectId, LaneName.Create("First"), order: null);

            var (created, lane) = await writeSvc.CreateAsync(
                projectId,
                LaneName.Create("Second"),
                order: null);

            created.Should().Be(DomainMutation.Created);
            lane!.Order.Should().Be(1);
        }

        [Fact]
        public async Task Create_Returns_Conflict_On_Duplicate_Name_In_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new LaneWriteService(repo, uow);

            var sameName = LaneName.Create("Dup");
            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var (firstResult, _) = await writeSvc.CreateAsync(projectId, sameName);
            firstResult.Should().Be(DomainMutation.Created);

            var (secondResult, lane2) = await writeSvc.CreateAsync(projectId, sameName);
            secondResult.Should().Be(DomainMutation.Conflict);
            lane2.Should().BeNull();
        }

        [Fact]
        public async Task Rename_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new LaneWriteService(repo, uow);

            var (_, laneId, _) = TestDataFactory.SeedProjectWithLane(db);

            var current = await db.Lanes.FirstAsync(l => l.Id == laneId);
            var result = await writeSvc.RenameAsync(
                laneId,
                LaneName.Create("New"),
                current.RowVersion);
            result.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task Reorder_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new LaneWriteService(repo, uow);

            var firstLaneName = "Lane A";
            var secondLaneName = "Lane B";
            var thirdLaneName = "Lane C";
            var (projectId, _, _) = TestDataFactory.SeedProjectWithLane(
                db,
                laneName: firstLaneName,
                order: 0);

            TestDataFactory.SeedLane(db, projectId, secondLaneName, order: 1);
            var laneC = TestDataFactory.SeedLane(db, projectId, thirdLaneName, order: 2);

            var current = await db.Lanes.FirstAsync(l => l.Id == laneC.Id);
            var result = await writeSvc.ReorderAsync(laneC.Id, 1, current.RowVersion);
            result.Should().Be(DomainMutation.Updated);

            var names = await db.Lanes
                                .AsNoTracking()
                                .Where(l => l.ProjectId == projectId)
                                .OrderBy(l => l.Order)
                                .Select(l => l.Name.Value)
                                .ToListAsync();

            names.Should().Equal(firstLaneName, thirdLaneName, secondLaneName);
        }

        [Fact]
        public async Task Delete_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new LaneWriteService(repo, uow);

            var (_, laneId, _) = TestDataFactory.SeedProjectWithLane(db);

            var current = await db.Lanes.FirstAsync(x => x.Id == laneId);
            var result = await writeSvc.DeleteAsync(laneId, current.RowVersion);
            result.Should().Be(DomainMutation.Deleted);
        }
    }
}
