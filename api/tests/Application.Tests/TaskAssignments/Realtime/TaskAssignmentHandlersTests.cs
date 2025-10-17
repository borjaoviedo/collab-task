using Application.Realtime;
using Application.TaskAssignments.Realtime;
using Domain.Enums;
using Moq;

namespace Application.Tests.TaskAssignments.Realtime
{
    public sealed class TaskAssignmentHandlersTests
    {
        [Fact]
        public async Task Created_calls_notifier_with_TaskAssignmentCreatedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var h = new TaskAssignmentChangedHandler(notifier.Object);
            var pid = Guid.NewGuid();
            var payload = new TaskAssignmentCreatedPayload(Guid.NewGuid(), Guid.NewGuid(), TaskRole.Owner);

            await h.Handle(new TaskAssignmentCreated(pid, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                pid,
                It.Is<BoardEvent<TaskAssignmentCreatedPayload>>(e =>
                    e.Type == "assignment.created" && e.ProjectId == pid && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task Updated_calls_notifier_with_TaskAssignmentUpdatedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var h = new TaskAssignmentChangedHandler(notifier.Object);
            var pid = Guid.NewGuid();
            var payload = new TaskAssignmentUpdatedPayload(Guid.NewGuid(), Guid.NewGuid(), TaskRole.CoOwner);

            await h.Handle(new TaskAssignmentUpdated(pid, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                pid,
                It.Is<BoardEvent<TaskAssignmentUpdatedPayload>>(e =>
                    e.Type == "assignment.updated" && e.ProjectId == pid && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task Removed_calls_notifier_with_TaskAssignmentRemovedEvent()
        {
            var notifier = new Mock<IBoardNotifier>();
            var h = new TaskAssignmentChangedHandler(notifier.Object);
            var pid = Guid.NewGuid();
            var payload = new TaskAssignmentRemovedPayload(Guid.NewGuid(), Guid.NewGuid());

            await h.Handle(new TaskAssignmentRemoved(pid, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                pid,
                It.Is<BoardEvent<TaskAssignmentRemovedPayload>>(e =>
                    e.Type == "assignment.removed" && e.ProjectId == pid && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }
    }
}
