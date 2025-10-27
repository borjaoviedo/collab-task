using Application.Realtime;
using MediatR;

namespace Application.TaskAssignments.Realtime
{
    public sealed class TaskAssignmentChangedHandler(IRealtimeNotifier notifier) :
    INotificationHandler<TaskAssignmentCreated>,
    INotificationHandler<TaskAssignmentUpdated>,
    INotificationHandler<TaskAssignmentRemoved>
    {
        public Task Handle(TaskAssignmentCreated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskAssignmentCreatedEvent(n.ProjectId, n.Payload), cancellationToken);
        public Task Handle(TaskAssignmentUpdated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskAssignmentUpdatedEvent(n.ProjectId, n.Payload), cancellationToken);
        public Task Handle(TaskAssignmentRemoved n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskAssignmentRemovedEvent(n.ProjectId, n.Payload), cancellationToken);
    }
}
