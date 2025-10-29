using Application.TaskNotes.Services;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.TaskNotes.Services
{
    public sealed class TaskNoteReadServiceTests
    {
        [Fact]
        public async Task GetAsync_Returns_TaskNote_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var readSvc = new TaskNoteReadService(repo);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await readSvc.GetAsync(noteId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(noteId);

            var notFound = await readSvc.GetAsync(noteId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_TaskNotes_In_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var readSvc = new TaskNoteReadService(repo);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var list = await readSvc.ListByTaskAsync(taskId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, taskId, userId);
            list = await readSvc.ListByTaskAsync(taskId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_Empty_List_When_Empty_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var readSvc = new TaskNoteReadService(repo);

            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            var list = await readSvc.ListByTaskAsync(taskId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_Empty_List_When_NotFound_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var readSvc = new TaskNoteReadService(repo);

            var list = await readSvc.ListByTaskAsync(taskId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_User_TaskNotes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var readSvc = new TaskNoteReadService(repo);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var list = await readSvc.ListByUserAsync(userId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, taskId, userId);
            list = await readSvc.ListByUserAsync(userId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_User_Without_Tasks()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var readSvc = new TaskNoteReadService(repo);

            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            var list = await readSvc.ListByUserAsync(taskId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_NotFound_User()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var readSvc = new TaskNoteReadService(repo);

            var list = await readSvc.ListByUserAsync(userId: Guid.NewGuid());
            list.Should().BeEmpty();
        }
    }
}
