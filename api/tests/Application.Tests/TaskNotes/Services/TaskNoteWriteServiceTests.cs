using Application.Abstractions.Time;
using Application.Common.Exceptions;
using Application.TaskActivities.Services;
using Application.TaskNotes.DTOs;
using Application.TaskNotes.Realtime;
using Application.TaskNotes.Services;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TestHelpers.Common;
using TestHelpers.Common.Fakes;
using TestHelpers.Common.Time;
using TestHelpers.Persistence;

namespace Application.Tests.TaskNotes.Services
{
    public sealed class TaskNoteWriteServiceTests
    {
        private static readonly IDateTimeProvider _clock = TestTime.FixedClock();

        [Fact]
        public async Task CreateAsync_Returns_Created_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);
            currentUser.UserId = userId;

            var createDto = new TaskNoteCreateDto { Content = "Content" };

            var readDto = await writeSvc.CreateAsync(
                projectId,
                taskId,
                createDto);

            readDto.Should().NotBeNull();
            readDto.TaskId.Should().Be(taskId);
            readDto.Content.Should().Be(createDto.Content);
            readDto.Id.Should().NotBe(Guid.Empty);

            mediator.Verify(m => m.Publish(
                    It.Is<TaskNoteCreated>(n =>
                        n.ProjectId == projectId &&
                        n.Payload.TaskId == taskId &&
                        n.Payload.NoteId == readDto.Id &&
                        n.Payload.Content == createDto.Content),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_And_Publishes_NoteUpdated()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, _, _, taskId, noteId, userId) = TestDataFactory.SeedFullBoard(db);
            currentUser.UserId = userId;

            var newContent = NoteContent.Create("New Content");

            mediator
                .Setup(m => m.Publish(
                        It.Is<TaskNoteUpdated>(n =>
                            n.ProjectId == projectId &&
                            n.Payload.NoteId == noteId &&
                            n.Payload.NewContent == newContent),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await writeSvc.EditAsync(
                projectId,
                taskId,
                noteId,
                new TaskNoteEditDto { NewContent = newContent.Value });

            result.Should().NotBeNull();
            result.Id.Should().Be(noteId);
            result.Content.Should().Be(newContent.Value);

            var after = await db.TaskNotes.AsNoTracking().SingleAsync();
            after.Content.Value.Should().Be(newContent);

            mediator.Verify(m => m.Publish(
                    It.Is<TaskNoteUpdated>(n =>
                        n.ProjectId == projectId &&
                        n.Payload.NoteId == noteId &&
                        n.Payload.NewContent == newContent),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteAsync_Publishes_NoteDeleted()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, _, _, taskId, noteId, userId) = TestDataFactory.SeedFullBoard(db);
            currentUser.UserId = userId;

            mediator
                .Setup(m => m.Publish(
                        It.Is<TaskNoteDeleted>(n =>
                            n.ProjectId == projectId &&
                            n.Payload.NoteId == noteId),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await writeSvc.DeleteAsync(
                projectId,
                taskId,
                noteId);

            mediator.Verify(m => m.Publish(
                    It.Is<TaskNoteDeleted>(n =>
                        n.ProjectId == projectId &&
                        n.Payload.NoteId == noteId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteAsync_When_Note_Id_Does_Not_Exist_Throws_NotFound_And_No_Publish()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            Func<Task> act = () => writeSvc.DeleteAsync(
                projectId: Guid.NewGuid(),
                taskId: Guid.NewGuid(),
                noteId: Guid.NewGuid());

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Task note not found*");

            mediator.Verify(m => m.Publish(
                    It.IsAny<INotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, TaskNoteWriteService Service, Mock<IMediator> Mediator, FakeCurrentUserService CurrentUser)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new TaskNoteRepository(db);
            var uow = new UnitOfWork(db);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var svc = new TaskNoteWriteService(
                repo,
                uow,
                activityWriteSvc,
                currentUser,
                mediator.Object);

            return Task.FromResult((db, svc, mediator, currentUser));
        }
    }
}
