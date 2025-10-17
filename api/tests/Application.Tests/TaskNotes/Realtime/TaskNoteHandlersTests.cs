using Application.Realtime;
using Application.TaskNotes.Realtime;
using Moq;

namespace Application.Tests.TaskNotes.Realtime
{
    public class TaskNoteHandlersTests
    {
        [Fact]
        public async Task CreatedHandler_calls_notifier_with_TaskNoteCreatedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var handler = new TaskNoteItemChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskNoteCreatedPayload(
                TaskId: Guid.NewGuid(),
                NoteId: Guid.NewGuid(),
                Content: "text",
                AuthorId: Guid.NewGuid(),
                CreatedAt: DateTimeOffset.UtcNow);

            await handler.Handle(new TaskNoteItemCreated(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<BoardEvent<TaskNoteCreatedPayload>>(e =>
                    e.Type == "tasknote.created" &&
                    e.ProjectId == projectId &&
                    e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }
    }
}
