using Application.Realtime;
using Application.TaskNotes.Realtime;
using Moq;
using TestHelpers.Common.Testing;

namespace Application.Tests.TaskNotes.Realtime
{
    [IntegrationTest]
    public class TaskNoteHandlersTests
    {
        [Fact]
        public async Task CreatedHandler_Calls_Notifier_With_NoteCreatedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var handler = new TaskNoteChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskNoteCreatedPayload(
                TaskId: Guid.NewGuid(),
                NoteId: Guid.NewGuid(),
                Content: "content");

            await handler.Handle(new TaskNoteCreated(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<RealtimeEvent<TaskNoteCreatedPayload>>(e =>
                    e.Type == "note.created" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task UpdatedHandler_Calls_Notifier_With_NoteUpdatedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var handler = new TaskNoteChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskNoteUpdatedPayload(
                TaskId: Guid.NewGuid(),
                NoteId: Guid.NewGuid(),
                NewContent: "new");

            await handler.Handle(new TaskNoteUpdated(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<RealtimeEvent<TaskNoteUpdatedPayload>>(e =>
                    e.Type == "note.updated" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task DeletedHandler_Calls_Notifier_With_NoteDeletedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var handler = new TaskNoteChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskNoteDeletedPayload(TaskId: Guid.NewGuid(), NoteId: Guid.NewGuid());

            await handler.Handle(new TaskNoteDeleted(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<RealtimeEvent<TaskNoteDeletedPayload>>(e =>
                    e.Type == "note.deleted" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }
    }
}
