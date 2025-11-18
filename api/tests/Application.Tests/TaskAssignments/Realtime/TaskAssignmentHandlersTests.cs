using Application.Realtime;
using Application.TaskAssignments.Realtime;
using Domain.Enums;
using Moq;
using TestHelpers.Common.Testing;

namespace Application.Tests.TaskAssignments.Realtime
{
    [IntegrationTest]
    public sealed class TaskAssignmentHandlersTests
    {
        [Fact]
        public async Task Created_Calls_Notifier_With_TaskAssignmentCreatedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var handler = new TaskAssignmentChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskAssignmentCreatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                Role: TaskRole.Owner);

            await handler.Handle(new TaskAssignmentCreated(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<RealtimeEvent<TaskAssignmentCreatedPayload>>(e =>
                    e.Type == "assignment.created" && e.ProjectId == projectId && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task Updated_Calls_Notifier_With_TaskAssignmentUpdatedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var handler = new TaskAssignmentChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskAssignmentUpdatedPayload(
                TaskId: Guid.NewGuid(),
                UserId: Guid.NewGuid(),
                NewRole: TaskRole.CoOwner);

            await handler.Handle(new TaskAssignmentUpdated(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<RealtimeEvent<TaskAssignmentUpdatedPayload>>(e =>
                    e.Type == "assignment.updated" && e.ProjectId == projectId && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }

        [Fact]
        public async Task Removed_Calls_Notifier_With_TaskAssignmentRemovedEvent()
        {
            var notifier = new Mock<IRealtimeNotifier>();
            var handler = new TaskAssignmentChangedHandler(notifier.Object);
            var projectId = Guid.NewGuid();
            var payload = new TaskAssignmentRemovedPayload(TaskId: Guid.NewGuid(), UserId: Guid.NewGuid());

            await handler.Handle(new TaskAssignmentRemoved(projectId, payload), CancellationToken.None);

            notifier.Verify(n => n.NotifyAsync(
                projectId,
                It.Is<RealtimeEvent<TaskAssignmentRemovedPayload>>(e =>
                    e.Type == "assignment.removed" && e.ProjectId == projectId && e.Payload == payload),
                It.IsAny<CancellationToken>()),
            Times.Once);
        }
    }
}
