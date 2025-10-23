using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

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

            var (_, _, _, tId, _, uId) = TestDataFactory.SeedFullBoard(db);

            var noteContent = NoteContent.Create("Note content");
            var note = TaskNote.Create(tId, uId, noteContent);
            await repo.AddAsync(note);
            await repo.SaveCreateChangesAsync();

            var fromDb = await db.TaskNotes.AsNoTracking().SingleAsync(n => n.Id == note.Id);
            fromDb.Content.Value.Should().Be(noteContent);
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_When_Content_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();

            var newContent = NoteContent.Create("New Content");
            var res = await repo.EditAsync(nId, newContent, noteFromDb.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            noteFromDb.Content.Value.Should().Be(newContent);
        }

        [Fact]
        public async Task EditAsync_Returns_NoOp_When_Content_Does_Not_Change()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var originalNoteContent = NoteContent.Create("Note content");
            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db, noteContent: originalNoteContent);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();

            var res = await repo.EditAsync(nId, originalNoteContent, noteFromDb.RowVersion);
            res.Should().Be(DomainMutation.NoOp);

            noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            noteFromDb.Content.Value.Should().Be(originalNoteContent);
        }

        [Fact]
        public async Task EditAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var originalNoteContent = NoteContent.Create("Note content");
            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db, noteContent: originalNoteContent);

            var res = await repo.EditAsync(nId, NoteContent.Create("New Content"), [1, 2]);
            res.Should().Be(DomainMutation.Conflict);

            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            noteFromDb.Content.Value.Should().Be(originalNoteContent);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Deleted_When_Note_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();

            var res = await repo.DeleteAsync(nId, noteFromDb.RowVersion);
            res.Should().Be(DomainMutation.Deleted);
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Note_Id_Does_Not_Exist()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var res = await repo.DeleteAsync(Guid.NewGuid(), [1, 2]);
            res.Should().Be(DomainMutation.NotFound);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);

            var res = await repo.DeleteAsync(nId, [1, 2]);
            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_TaskNote_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await repo.GetByIdAsync(nId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(nId);

            var notFound = await repo.GetByIdAsync(Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task GetTrackedByIdAsync_Returns_TaskNote_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await repo.GetTrackedByIdAsync(nId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(nId);

            var notFound = await repo.GetTrackedByIdAsync(Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_TaskNotes_In_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, tId, _, uId) = TestDataFactory.SeedFullBoard(db);

            var list = await repo.ListByTaskAsync(tId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, tId, uId);
            list = await repo.ListByTaskAsync(tId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_Empty_List_When_Empty_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, tId) = TestDataFactory.SeedColumnWithTask(db);

            var list = await repo.ListByTaskAsync(tId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByColumnAsync_Returns_Empty_List_When_NotFound_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var list = await repo.ListByTaskAsync(Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Author_TaskNotes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, tId, _, uId) = TestDataFactory.SeedFullBoard(db);

            var list = await repo.ListByUserAsync(uId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, tId, uId);
            list = await repo.ListByUserAsync(uId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_Author_Without_Tasks()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var (_, _, _, tId) = TestDataFactory.SeedColumnWithTask(db);

            var list = await repo.ListByUserAsync(tId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_NotFound_Author()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            var list = await repo.ListByUserAsync(Guid.NewGuid());
            list.Should().BeEmpty();
        }
    }
}
