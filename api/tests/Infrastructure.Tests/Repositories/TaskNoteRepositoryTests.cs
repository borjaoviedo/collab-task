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
        public async Task UpdateAsync_Marks_Entity_Modified_And_Persists_Changes()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);
            var note = await repo.GetByIdForUpdateAsync(noteId);

            // Modify through domain behavior
            note!.Edit(NoteContent.Create("Updated Content"));

            await repo.UpdateAsync(note);
            await db.SaveChangesAsync();

            var reloaded = await db.TaskNotes
                .AsNoTracking()
                .FirstOrDefaultAsync(tn => tn.Id == noteId);

            reloaded.Should().NotBeNull();
            reloaded!.Content.Value.Should().Be("Updated Content");
        }

        [Fact]
        public async Task RemoveAsync_Deletes_User_On_SaveChanges()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);
            var note = await repo.GetByIdForUpdateAsync(noteId);

            await repo.RemoveAsync(note!);
            await db.SaveChangesAsync();

            var reloaded = await db.TaskNotes
                .AsNoTracking()
                .FirstOrDefaultAsync(tn => tn.Id == noteId);

            reloaded.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Returns_TaskNote_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await repo.GetByIdAsync(noteId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(noteId);

            var notFound = await repo.GetByIdAsync(noteId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_Returns_TaskNote_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var (_, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);

            var existing = await repo.GetByIdForUpdateAsync(noteId);
            existing.Should().NotBeNull();
            existing.Id.Should().Be(noteId);

            var notFound = await repo.GetByIdForUpdateAsync(noteId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByTaskIdAsync_Returns_TaskNotes_In_Column()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var list = await repo.ListByTaskIdAsync(taskId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, taskId, userId);
            list = await repo.ListByTaskIdAsync(taskId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByTaskIdAsync_Returns_Empty_List_When_Empty_Task()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var (_, _, _, taskId, _) = TestDataFactory.SeedColumnWithTask(db);

            var list = await repo.ListByTaskIdAsync(taskId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_Author_TaskNotes()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var list = await repo.ListByUserIdAsync(userId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedTaskNote(db, taskId, userId);
            list = await repo.ListByUserIdAsync(userId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_Empty_List_When_Author_Without_Tasks()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var (_, _, _, taskId, _) = TestDataFactory.SeedColumnWithTask(db);

            var list = await repo.ListByUserIdAsync(taskId);
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_Empty_List_When_NotFound_Author()
        {
            using var dbh = new SqliteTestDb();
            var (_, repo) = await CreateSutAsync(dbh);

            var list = await repo.ListByUserIdAsync(userId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, TaskNoteRepository Repo)>
            CreateSutAsync(SqliteTestDb dbh)
        {
            var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);

            return Task.FromResult((db, repo));
        }
    }
}
