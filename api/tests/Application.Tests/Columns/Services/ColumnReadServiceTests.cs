using Application.Columns.Services;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Application.Tests.Columns.Services
{
    public sealed class ColumnReadServiceTests
    {
        [Fact]
        public async Task GetAsync_Returns_Column_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var readSvc = new ColumnReadService(repo);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var existing = await readSvc.GetAsync(column.Id);
            existing.Should().NotBeNull();

            var notFound = await readSvc.GetAsync(Guid.NewGuid());
            notFound.Should().Be(null);
        }

        [Fact]
        public async Task ListByLaneAsync_Returns_Existing_Columns_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var readSvc = new ColumnReadService(repo);

            var (projectId, laneId) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, projectId, laneId);

            var list = await readSvc.ListByLaneAsync(laneId);
            list.Should().HaveCount(1);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 1);
            list = await readSvc.ListByLaneAsync(laneId);
            list.Should().HaveCount(2);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 3);
            list = await readSvc.ListByLaneAsync(laneId);
            list.Should().HaveCount(3);
        }

        [Fact]
        public async Task ListByLaneAsync_Returns_Empty_List_When_No_Column_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var readSvc = new ColumnReadService(repo);

            var (_, laneId) = TestDataFactory.SeedProjectWithLane(db);
            var list = await readSvc.ListByLaneAsync(laneId);
            list.Should().BeEmpty();
        }
    }
}
