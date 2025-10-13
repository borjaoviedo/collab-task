using Application.TaskItems.Services;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.TaskItems.Services
{
    public sealed class TaskItemReadServiceTests
    {
        [Fact]
        public async Task GetAsync_Returns_TaskItem_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemReadService(repo);

            var (_, _, _, tId) = TestDataFactory.SeedColumnWithTask(db);

            var existing = await svc.GetAsync(tId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(tId);

            var notFound = await svc.GetAsync(Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByColumnAsync_Returns_TaskItems_In_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemReadService(repo);

            var (pId, lId, cId, _) = TestDataFactory.SeedColumnWithTask(db);

            var list = await svc.ListByColumnAsync(cId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskItem(db, pId, lId, cId, sortKey: 1);
            list = await svc.ListByColumnAsync(cId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByColumnAsync_Returns_Empty_List_When_Empty_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemReadService(repo);

            var (_, _, cId) = TestDataFactory.SeedLaneWithColumn(db);

            var list = await svc.ListByColumnAsync(cId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByColumnAsync_Returns_Empty_List_When_NotFound_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemReadService(repo);

            TestDataFactory.SeedColumnWithTask(db);

            var list = await svc.ListByColumnAsync(Guid.NewGuid());
            list.Should().BeEmpty();
        }
    }
}
