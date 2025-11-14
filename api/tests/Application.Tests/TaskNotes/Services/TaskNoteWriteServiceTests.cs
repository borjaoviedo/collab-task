using Application.Abstractions.Time;
using Application.TaskActivities.Services;
using Application.TaskNotes.Realtime;
using Application.TaskNotes.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TestHelpers.Common;
using TestHelpers.Common.Time;
using TestHelpers.Persistence;

namespace Application.Tests.TaskNotes.Services
{
    public sealed class TaskNoteWriteServiceTests
    {
        private readonly IDateTimeProvider _clock = TestTime.FixedClock();

        [Fact]
        public async Task CreateAsync_Returns_Created_And_Id_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var noteWriteSvc = new TaskNoteWriteService(
                repo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var (result, note) = await noteWriteSvc.CreateAsync(
                projectId,
                taskId,
                userId,
                NoteContent.Create("content"));

            result.Should().Be(DomainMutation.Created);
            note.Should().NotBeNull();

            mediator.Verify(m => m.Publish(
                It.Is<TaskNoteCreated>(n =>
                    n.ProjectId == projectId &&
                    n.Payload.TaskId == taskId &&
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
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>(MockBehavior.Strict);

            var noteWriteSvc = new TaskNoteWriteService(
                repo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, _, _, taskId, noteId, _) = TestDataFactory.SeedFullBoard(db);
            var noteFromDb = await db.TaskNotes.AsNoTracking().SingleAsync();
            var user = TestDataFactory.SeedUser(db);

            var newContent = NoteContent.Create("New Content");

            mediator
                .Setup(m => m.Publish(
                    It.Is<TaskNoteUpdated>(n => n.ProjectId == projectId &&
                                                n.Payload.NoteId == noteId &&
                                                n.Payload.NewContent == newContent),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await noteWriteSvc.EditAsync(
                projectId,
                taskId,
                noteId,
                user.Id,
                newContent,
                noteFromDb.RowVersion);

            result.Should().Be(DomainMutation.Updated);

            var after = await db.TaskNotes.AsNoTracking().SingleAsync();
            after.Content.Value.Should().Be(newContent);

            mediator.Verify(m => m.Publish(
                It.Is<TaskNoteUpdated>(n => n.ProjectId == projectId &&
                                            n.Payload.NoteId == noteId &&
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
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var noteWriteSvc = new TaskNoteWriteService(
                repo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var original = NoteContent.Create("Note content");
            var (projectId, _, _, taskId, noteId, _) = TestDataFactory.SeedFullBoard(
                db,
                noteContent: original);
            var note = await db.TaskNotes.AsNoTracking().SingleAsync();
            var user = TestDataFactory.SeedUser(db);

            var result = await noteWriteSvc.EditAsync(
                projectId,
                taskId,
                noteId,
                user.Id,
                original,
                note.RowVersion);
            result.Should().Be(DomainMutation.NoOp);

            mediator.Verify(m => m.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EditAsync_Returns_Conflict_On_RowVersion_Mismatch_No_Publish()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var noteWriteSvc = new TaskNoteWriteService(
                repo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var original = NoteContent.Create("Note content");
            var (projectId, _, _, taskId, noteId, _) = TestDataFactory.SeedFullBoard(
                db,
                noteContent: original);
            var user = TestDataFactory.SeedUser(db);

            var result = await noteWriteSvc.EditAsync(
                projectId,
                taskId,
                noteId,
                user.Id,
                NoteContent.Create("New Content"),
                rowVersion: [1, 2]);
            result.Should().Be(DomainMutation.Conflict);

            var note = await db.TaskNotes.AsNoTracking().SingleAsync();
            note.Content.Value.Should().Be(original);

            mediator.Verify(m => m.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Deleted_And_Publishes_NoteDeleted()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>(MockBehavior.Strict);

            var noteWriteSvc = new TaskNoteWriteService(
                repo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);
            var note = await db.TaskNotes.AsNoTracking().SingleAsync();
            var user = TestDataFactory.SeedUser(db);

            mediator
                .Setup(m => m.Publish(
                    It.Is<TaskNoteDeleted>(n => n.ProjectId == projectId && n.Payload.NoteId == noteId),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await noteWriteSvc.DeleteAsync(
                projectId,
                noteId,
                user.Id,
                note.RowVersion);

            result.Should().Be(DomainMutation.Deleted);
            mediator.Verify(m => m.Publish(
                It.Is<TaskNoteDeleted>(n => n.ProjectId == projectId && n.Payload.NoteId == noteId),
                It.IsAny<CancellationToken>()), Times.Once);
            mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Note_Id_Does_Not_Exist_No_Publish()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var noteWriteSvc = new TaskNoteWriteService(
                repo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var result = await noteWriteSvc.DeleteAsync(
                projectId: Guid.NewGuid(),
                noteId: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                rowVersion: [1, 2]);
            result.Should().Be(DomainMutation.NotFound);

            mediator.Verify(m => m.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_On_RowVersion_Mismatch_No_Publish()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var noteWriteSvc = new TaskNoteWriteService(
                repo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, _, _, _, noteId, _) = TestDataFactory.SeedFullBoard(db);
            var user = TestDataFactory.SeedUser(db);

            var result = await noteWriteSvc.DeleteAsync(
                projectId,
                noteId,
                user.Id,
                rowVersion: [1, 2]);
            result.Should().Be(DomainMutation.Conflict);

            mediator.Verify(m => m.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
