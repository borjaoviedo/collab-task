using Application.Common.Exceptions;
using Application.TaskNotes.Services;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Fakes;
using TestHelpers.Persistence;

namespace Application.Tests.TaskNotes.Services
{
    public sealed class TaskNoteReadServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_TaskNote_When_Exists_Otherwise_Throws()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await readSvc.GetByIdAsync(noteId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(noteId);

            await FluentActions.Invoking(() =>
                readSvc.GetByIdAsync(noteId: Guid.NewGuid()))
                .Should()
                .ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ListByTaskIdAsync_Returns_TaskNotes_In_Task()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var list = await readSvc.ListByTaskIdAsync(taskId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, taskId, userId);

            list = await readSvc.ListByTaskIdAsync(taskId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByTaskIdAsync_Returns_Empty_List_When_Empty_Task()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (_, _, _, taskId, _) = TestDataFactory.SeedColumnWithTask(db);

            var list = await readSvc.ListByTaskIdAsync(taskId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByTaskIdAsync_Returns_Empty_List_When_NotFound_Task()
        {
            using var dbh = new SqliteTestDb();
            var (_, readSvc, _) = await CreateSutAsync(dbh);

            var list = await readSvc.ListByTaskIdAsync(taskId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_User_TaskNotes()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var list = await readSvc.ListByUserIdAsync(userId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, taskId, userId);

            list = await readSvc.ListByUserIdAsync(userId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_Empty_List_When_User_Without_TaskNotes()
        {
            using var dbh = new SqliteTestDb();
            var (_, readSvc, _) = await CreateSutAsync(dbh);

            var userId = Guid.NewGuid();

            var list = await readSvc.ListByUserIdAsync(userId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_Empty_List_When_NotFound_User()
        {
            using var dbh = new SqliteTestDb();
            var (_, readSvc, _) = await CreateSutAsync(dbh);

            var list = await readSvc.ListByUserIdAsync(userId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, TaskNoteReadService Service, FakeCurrentUserService CurrentUser)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };

            var svc = new TaskNoteReadService(repo, currentUser);
            return Task.FromResult((db, svc, currentUser));
        }
    }
}
