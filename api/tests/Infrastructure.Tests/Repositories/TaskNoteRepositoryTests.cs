using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Repositories
{
    public sealed class TaskNoteRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Persists_TaskNote()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var noteContent = NoteContent.Create("Note content");
            var note = TaskNote.Create(taskId, userId, noteContent);

            await repo.AddAsync(note);
            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await db.TaskNotes
                .AsNoTracking()
                .SingleAsync(n => n.Id == note.Id);
            fromDb.Content.Value.Should().Be(noteContent);
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_When_Content_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();

            var newContent = NoteContent.Create("New Content");
            var result = await repo.EditAsync(noteId, newContent, noteFromDb.RowVersion);
            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Update);

            noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            noteFromDb.Content.Value.Should().Be(newContent);
        }

        [Fact]
        public async Task EditAsync_Returns_NoOp_When_Content_Does_Not_Change()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);

            var originalNoteContent = NoteContent.Create("Note content");
            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db, noteContent: originalNoteContent);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();

            var result = await repo.EditAsync(noteId, originalNoteContent, noteFromDb.RowVersion);
            result.Should().Be(PrecheckStatus.NoOp);

            await uow.SaveAsync(MutationKind.Update);

            noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            noteFromDb.Content.Value.Should().Be(originalNoteContent);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Ready_When_Note_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();

            var result = await repo.DeleteAsync(noteId, noteFromDb.RowVersion);
            result.Should().Be(PrecheckStatus.Ready);
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Note_Id_Does_Not_Exist()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var result = await repo.DeleteAsync(noteId: Guid.NewGuid(), rowVersion: [1, 2]);
            result.Should().Be(PrecheckStatus.NotFound);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_TaskNote_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await repo.GetByIdAsync(noteId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(noteId);

            var notFound = await repo.GetByIdAsync(noteId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task GetTrackedByIdAsync_Returns_TaskNote_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await repo.GetTrackedByIdAsync(noteId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(noteId);

            var notFound = await repo.GetTrackedByIdAsync(noteId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_TaskNotes_In_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var list = await repo.ListByTaskAsync(taskId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, taskId, userId);
            list = await repo.ListByTaskAsync(taskId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_Empty_List_When_Empty_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, taskId, _) = TestDataFactory.SeedColumnWithTask(db);

            var list = await repo.ListByTaskAsync(taskId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByColumnAsync_Returns_Empty_List_When_NotFound_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var list = await repo.ListByTaskAsync(taskId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Author_TaskNotes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var list = await repo.ListByUserAsync(userId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, taskId, userId);
            list = await repo.ListByUserAsync(userId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_Author_Without_Tasks()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, taskId, _) = TestDataFactory.SeedColumnWithTask(db);

            var list = await repo.ListByUserAsync(taskId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_NotFound_Author()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var list = await repo.ListByUserAsync(userId: Guid.NewGuid());
            list.Should().BeEmpty();
        }
    }
}
