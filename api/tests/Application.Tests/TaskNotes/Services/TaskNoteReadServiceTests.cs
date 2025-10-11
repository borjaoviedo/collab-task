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
            var svc = new TaskNoteReadService(repo);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await svc.GetAsync(nId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(nId);

            var notFound = await svc.GetAsync(Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_TaskNotes_In_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var svc = new TaskNoteReadService(repo);

            var (_, _, _, tId, _, uId) = TestDataFactory.SeedFullBoard(db);

            var list = await svc.ListByTaskAsync(tId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, tId, uId);
            list = await svc.ListByTaskAsync(tId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_Empty_List_When_Empty_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var svc = new TaskNoteReadService(repo);

            var (_, _, _, tId) = TestDataFactory.SeedColumnWithTask(db);

            var list = await svc.ListByTaskAsync(tId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByColumnAsync_Returns_Empty_List_When_NotFound_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var svc = new TaskNoteReadService(repo);

            var list = await svc.ListByTaskAsync(Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByAuthorAsync_Returns_Author_TaskNotes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var svc = new TaskNoteReadService(repo);

            var (_, _, _, tId, _, uId) = TestDataFactory.SeedFullBoard(db);

            var list = await svc.ListByAuthorAsync(uId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, tId, uId);
            list = await svc.ListByAuthorAsync(uId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByAuthorAsync_Returns_Empty_List_When_Author_Without_Tasks()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var svc = new TaskNoteReadService(repo);

            var (_, _, _, tId) = TestDataFactory.SeedColumnWithTask(db);

            var list = await svc.ListByAuthorAsync(tId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByAuthorAsync_Returns_Empty_List_When_NotFound_Author()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var svc = new TaskNoteReadService(repo);

            var list = await svc.ListByAuthorAsync(Guid.NewGuid());
            list.Should().BeEmpty();
        }
    }
}
