using Application.Realtime;
using Application.TaskAssignments.Realtime;
using Domain.Enums;
using Moq;

namespace Application.Tests.TaskAssignments.Realtime
{
    public sealed class TaskAssignmentHandlersTests
    {
        [Fact]
        public async Task Created_Calls_Notifier_With_TaskAssignmentCreatedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var h = new TaskAssignmentChangedHandler(notifier.Object);
            var pid = Guid.NewGuid();
            var payload = new TaskAssignmentCreatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                Role: TaskRole.Owner);

            await h.Handle(new TaskAssignmentCreated(pid, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                pid,
                It.Is<RealtimeEvent<TaskAssignmentCreatedPayload>>(e =>
                    e.Type == "assignment.created" && e.ProjectId == pid && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task Updated_Calls_Notifier_With_TaskAssignmentUpdatedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var h = new TaskAssignmentChangedHandler(notifier.Object);
            var pid = Guid.NewGuid();
            var payload = new TaskAssignmentUpdatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                NewRole: TaskRole.CoOwner);

            await h.Handle(new TaskAssignmentUpdated(pid, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                pid,
                It.Is<RealtimeEvent<TaskAssignmentUpdatedPayload>>(e =>
                    e.Type == "assignment.updated" && e.ProjectId == pid && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task Removed_Calls_Notifier_With_TaskAssignmentRemovedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var h = new TaskAssignmentChangedHandler(notifier.Object);
            var pid = Guid.NewGuid();
            var payload = new TaskAssignmentRemovedPayload(TaskId: Guid.NewGuid(), UserId: Guid.NewGuid());

            await h.Handle(new TaskAssignmentRemoved(pid, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                pid,
                It.Is<RealtimeEvent<TaskAssignmentRemovedPayload>>(e =>
                    e.Type == "assignment.removed" && e.ProjectId == pid && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }
    }
}
