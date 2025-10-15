using Application.TaskActivities.Services;
using Application.TaskNotes.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Application.Tests.TaskNotes.Services
{
    public sealed class TaskNoteWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Returns_Created_And_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var svc = new TaskNoteWriteService(repo, actSvc);

            var (_, _, _, tId, _, uId) = TestDataFactory.SeedFullBoard(db);
            var (res, id) = await svc.CreateAsync(tId, uId, "content");

            res.Should().Be(DomainMutation.Created);
            id.Should().NotBeNull();
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_When_Content_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var svc = new TaskNoteWriteService(repo, actSvc);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            var user = TestDataFactory.SeedUser(db);

            var newContent = "New Content";
            var res = await svc.EditAsync(nId, user.Id, newContent, noteFromDb.RowVersion);
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
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var svc = new TaskNoteWriteService(repo, actSvc);
           
            var originalNoteContent = "Note content";
            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db, noteContent: originalNoteContent);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            var user = TestDataFactory.SeedUser(db);

            var res = await svc.EditAsync(nId, user.Id, originalNoteContent, noteFromDb.RowVersion);
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
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var svc = new TaskNoteWriteService(repo, actSvc);

            var originalNoteContent = "Note content";
            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db, noteContent: originalNoteContent);

            var user = TestDataFactory.SeedUser(db);
            var res = await svc.EditAsync(nId, user.Id, "New Content", [1, 2]);

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
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var svc = new TaskNoteWriteService(repo, actSvc);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();

            var user = TestDataFactory.SeedUser(db);
            var res = await svc.DeleteAsync(nId, user.Id, noteFromDb.RowVersion);
            res.Should().Be(DomainMutation.Deleted);
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Note_Id_Does_Not_Exist()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var svc = new TaskNoteWriteService(repo, actSvc);

            var res = await svc.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), [1, 2]);
            res.Should().Be(DomainMutation.NotFound);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var svc = new TaskNoteWriteService(repo, actSvc);

            var (_, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);

            var user = TestDataFactory.SeedUser(db);
            var res = await svc.DeleteAsync(nId, user.Id, [1, 2]);
            res.Should().Be(DomainMutation.Conflict);
        }
    }
}
