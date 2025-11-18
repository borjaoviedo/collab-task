using Application.Columns.Services;
using Application.Common.Exceptions;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Application.Tests.Columns.Services
{
    [IntegrationTest]
    public sealed class ColumnReadServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_Column_When_Exists_Otherwise_Throws()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, projectId, laneId);

            var existing = await readSvc.GetByIdAsync(column.Id);
            existing.Should().NotBeNull();

            await FluentActions.Invoking(() =>
                readSvc.GetByIdAsync(Guid.NewGuid()))
                .Should()
                .ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ListByLaneIdAsync_Returns_Existing_Columns_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var (projectId, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, projectId, laneId);

            var list = await readSvc.ListByLaneIdAsync(laneId);
            list.Should().HaveCount(1);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 1);
            list = await readSvc.ListByLaneIdAsync(laneId);
            list.Should().HaveCount(2);

            TestDataFactory.SeedColumn(db, projectId, laneId, order: 3);
            list = await readSvc.ListByLaneIdAsync(laneId);
            list.Should().HaveCount(3);
        }

        [Fact]
        public async Task ListByLaneIdAsync_Returns_Empty_List_When_No_Column_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var (_, laneId, _) = TestDataFactory.SeedProjectWithLane(db);
            var list = await readSvc.ListByLaneIdAsync(laneId);
            list.Should().BeEmpty();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, ColumnReadService Service)> CreateSutAsync(SqliteTestDb dbh)
        {
            var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);

            var svc = new ColumnReadService(repo);

            return Task.FromResult((db, svc));
        }
    }
}
