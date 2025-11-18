using Application.Common.Exceptions;
using Application.TaskItems.Services;
using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Application.Tests.TaskItems.Services
{
    [IntegrationTest]
    public sealed class TaskItemReadServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_TaskItem_When_Exists_Otherwise_Throws()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var readSvc = new TaskItemReadService(repo);

            var (_, _, _, taskId, _) = TestDataFactory.SeedColumnWithTask(db);

            var existing = await readSvc.GetByIdAsync(taskId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(taskId);

            await FluentActions.Invoking(() =>
                readSvc.GetByIdAsync(Guid.NewGuid()))
                .Should()
                .ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ListByColumnIdAsync_Returns_TaskItems_In_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var readSvc = new TaskItemReadService(repo);

            var (projectId, laneId, columnId, _, _) = TestDataFactory.SeedColumnWithTask(db);

            var list = await readSvc.ListByColumnIdAsync(columnId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, sortKey: 1);
            list = await readSvc.ListByColumnIdAsync(columnId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByColumnIdAsync_Returns_Empty_List_When_Empty_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var readSvc = new TaskItemReadService(repo);

            var (_, _, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);

            var list = await readSvc.ListByColumnIdAsync(columnId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByColumnIdAsync_Returns_Empty_List_When_NotFound_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var readSvc = new TaskItemReadService(repo);

            TestDataFactory.SeedColumnWithTask(db);

            var list = await readSvc.ListByColumnIdAsync(columnId: Guid.NewGuid());
            list.Should().BeEmpty();
        }
    }
}
