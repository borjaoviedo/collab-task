using Application.Realtime;
using Application.TaskNotes.Realtime;
using Moq;

namespace Application.Tests.TaskNotes.Realtime
{
    public class TaskNoteHandlersTests
    {
        [Fact]
        public async Task CreatedHandler_calls_notifier_with_NoteCreatedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var handler = new TaskNoteChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskNoteCreatedPayload(Guid.NewGuid(), Guid.NewGuid(), "c");

            await handler.Handle(new TaskNoteCreated(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<BoardEvent<TaskNoteCreatedPayload>>(e =>
                    e.Type == "note.created" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task UpdatedHandler_calls_notifier_with_NoteUpdatedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var handler = new TaskNoteChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskNoteUpdatedPayload(Guid.NewGuid(), "new");

            await handler.Handle(new TaskNoteUpdated(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<BoardEvent<TaskNoteUpdatedPayload>>(e =>
                    e.Type == "note.updated" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task DeletedHandler_calls_notifier_with_NoteDeletedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var handler = new TaskNoteChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskNoteDeletedPayload(Guid.NewGuid());

            await handler.Handle(new TaskNoteDeleted(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<BoardEvent<TaskNoteDeletedPayload>>(e =>
                    e.Type == "note.deleted" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }
    }
}
