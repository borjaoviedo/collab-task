using Application.TaskActivities.Services;
using Application.TaskNotes.Realtime;
using Application.TaskNotes.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TestHelpers;

namespace Application.Tests.TaskNotes.Services
{
    public sealed class TaskNoteWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Returns_Created_And_Id_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskNoteWriteService(repo, actSvc, mediator.Object);

            var (pId, _, _, tId, _, uId) = TestDataFactory.SeedFullBoard(db);

            var (res, note) = await svc.CreateAsync(pId, tId, uId, "content");

            res.Should().Be(DomainMutation.Created);
            note.Should().NotBeNull();

            mediator.Verify(m => m.Publish(
                It.Is<TaskNoteCreated>(n =>
                    n.ProjectId == pId &&
                    n.Payload.TaskId == tId &&
                    n.Payload.NoteId == note.Id &&
                    n.Payload.Content == "content"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_And_Publishes_NoteUpdated()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>(MockBehavior.Strict);
            var svc = new TaskNoteWriteService(repo, actSvc, mediator.Object);

            var (pId, _, _, tId, nId, _) = TestDataFactory.SeedFullBoard(db);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            var user = TestDataFactory.SeedUser(db);

            const string newContent = "New Content";

            mediator
                .Setup(m => m.Publish(
                    It.Is<TaskNoteUpdated>(n => n.ProjectId == pId &&
                                                n.Payload.NoteId == nId &&
                                                n.Payload.NewContent == newContent),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var res = await svc.EditAsync(pId, tId, nId, user.Id, newContent, noteFromDb.RowVersion);

            res.Should().Be(DomainMutation.Updated);

            var after = await db.TaskNotes.AsNoTracking().SingleAsync();
            after.Content.Value.Should().Be(newContent);

            mediator.Verify(m => m.Publish(
                It.Is<TaskNoteUpdated>(n => n.ProjectId == pId &&
                                            n.Payload.NoteId == nId &&
                                            n.Payload.NewContent == newContent),
                It.IsAny<CancellationToken>()), Times.Once);
            mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task EditAsync_Returns_NoOp_When_Content_Does_Not_Change_No_Publish()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskNoteWriteService(repo, actSvc, mediator.Object);

            var original = "Note content";
            var (pId, _, _, tId, nId, _) = TestDataFactory.SeedFullBoard(db, noteContent: original);
            var note = await db.TaskNotes.AsNoTracking().SingleAsync();
            var user = TestDataFactory.SeedUser(db);

            var res = await svc.EditAsync(pId, tId, nId, user.Id, original, note.RowVersion);
            res.Should().Be(DomainMutation.NoOp);

            mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EditAsync_Returns_Conflict_On_RowVersion_Mismatch_No_Publish()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskNoteWriteService(repo, actSvc, mediator.Object);

            var original = "Note content";
            var (pId, _, _, tId, nId, _) = TestDataFactory.SeedFullBoard(db, noteContent: original);
            var user = TestDataFactory.SeedUser(db);

            var res = await svc.EditAsync(pId, tId, nId, user.Id, "New Content", [1, 2]);
            res.Should().Be(DomainMutation.Conflict);

            var note = await db.TaskNotes.AsNoTracking().SingleAsync();
            note.Content.Value.Should().Be(original);

            mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Deleted_And_Publishes_NoteDeleted()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>(MockBehavior.Strict);
            var svc = new TaskNoteWriteService(repo, actSvc, mediator.Object);

            var (pId, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);
            var note = await db.TaskNotes.AsNoTracking().SingleAsync();
            var user = TestDataFactory.SeedUser(db);

            mediator
                .Setup(m => m.Publish(
                    It.Is<TaskNoteDeleted>(n => n.ProjectId == pId && n.Payload.NoteId == nId),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var res = await svc.DeleteAsync(pId, nId, user.Id, note.RowVersion);

            res.Should().Be(DomainMutation.Deleted);
            mediator.Verify(m => m.Publish(
                It.Is<TaskNoteDeleted>(n => n.ProjectId == pId && n.Payload.NoteId == nId),
                It.IsAny<CancellationToken>()), Times.Once);
            mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Note_Id_Does_Not_Exist_No_Publish()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskNoteWriteService(repo, actSvc, mediator.Object);

            var res = await svc.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),[1, 2]);
            res.Should().Be(DomainMutation.NotFound);

            mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_On_RowVersion_Mismatch_No_Publish()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskNoteWriteService(repo, actSvc, mediator.Object);

            var (pId, _, _, _, nId, _) = TestDataFactory.SeedFullBoard(db);
            var user = TestDataFactory.SeedUser(db);

            var res = await svc.DeleteAsync(pId, nId, user.Id, [1, 2]);
            res.Should().Be(DomainMutation.Conflict);

            mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
