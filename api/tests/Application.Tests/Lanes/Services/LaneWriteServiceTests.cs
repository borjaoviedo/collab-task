using Application.Common.Exceptions;
using Application.Lanes.DTOs;
using Application.Lanes.Services;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Application.Tests.Lanes.Services
{
    [IntegrationTest]
    public sealed class LaneWriteServiceTests
    {
        [Fact]
        public async Task Create_Throws_Conflict_On_Duplicate_Name_In_Project()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, _) = await CreateSutAsync(dbh);

            var sameName = "Dup";
            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var dto1 = new LaneCreateDto { Name = sameName, Order = 0 };
            await writeSvc.CreateAsync(projectId, dto1);

            var dto2 = new LaneCreateDto { Name = sameName, Order = 1 };

            var act = async () => await writeSvc.CreateAsync(projectId, dto2);

            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task Rename_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, _) = await CreateSutAsync(dbh);

            var (_, laneId, _) = TestDataFactory.SeedProjectWithLane(db);

            var dto = new LaneRenameDto { NewName = "New" };
            var result = await writeSvc.RenameAsync(laneId, dto);

            result.Should().NotBeNull();
            result.Id.Should().Be(laneId);
            result.Name.Should().Be("New");
        }

        [Fact]
        public async Task Reorder_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, _) = await CreateSutAsync(dbh);

            var firstLaneName = "Lane A";
            var secondLaneName = "Lane B";
            var thirdLaneName = "Lane C";

            var (projectId, _, _) = TestDataFactory.SeedProjectWithLane(
                db,
                laneName: firstLaneName,
                order: 0);

            TestDataFactory.SeedLane(db, projectId, secondLaneName, order: 1);
            var laneC = TestDataFactory.SeedLane(db, projectId, thirdLaneName, order: 2);

            var dto = new LaneReorderDto { NewOrder = 1 };
            var result = await writeSvc.ReorderAsync(laneC.Id, dto);

            result.Should().NotBeNull();
            result.Id.Should().Be(laneC.Id);
            result.Order.Should().Be(1);

            var names = await db.Lanes
                .AsNoTracking()
                .Where(l => l.ProjectId == projectId)
                .OrderBy(l => l.Order)
                .Select(l => l.Name.Value)
                .ToListAsync();

            names.Should().Equal(firstLaneName, thirdLaneName, secondLaneName);
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, LaneWriteService Service, LaneRepository Repo)>
            CreateSutAsync(SqliteTestDb dbh)
        {
            var db = dbh.CreateContext();
            var repo = new LaneRepository(db);
            var uow = new UnitOfWork(db);

            var svc = new LaneWriteService(repo, uow);

            return Task.FromResult((db, svc, repo));
        }
    }
}
