using Application.Columns.Services;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

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
            var svc = new ColumnReadService(repo);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            var column = TestDataFactory.SeedColumn(db, pId, lId);

            var existing = await svc.GetAsync(column.Id);
            existing.Should().NotBeNull();

            var notFound = await svc.GetAsync(Guid.NewGuid());
            notFound.Should().Be(null);
        }

        [Fact]
        public async Task ListByLaneAsync_Returns_Existing_Columns_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var svc = new ColumnReadService(repo);

            var (pId, lId) = TestDataFactory.SeedProjectWithLane(db);
            TestDataFactory.SeedColumn(db, pId, lId);

            var list = await svc.ListByLaneAsync(lId);
            list.Should().HaveCount(1);

            TestDataFactory.SeedColumn(db, pId, lId, order: 1);
            list = await svc.ListByLaneAsync(lId);
            list.Should().HaveCount(2);

            TestDataFactory.SeedColumn(db, pId, lId, order: 3);
            list = await svc.ListByLaneAsync(lId);
            list.Should().HaveCount(3);
        }

        [Fact]
        public async Task ListByLaneAsync_Returns_Empty_List_When_No_Column_In_Lane()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ColumnRepository(db);
            var svc = new ColumnReadService(repo);

            var (_, lId) = TestDataFactory.SeedProjectWithLane(db);
            var list = await svc.ListByLaneAsync(lId);
            list.Should().BeEmpty();
        }
    }
}
