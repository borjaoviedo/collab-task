using Application.Realtime;
using MediatR;

namespace Application.TaskAssignments.Realtime
{
    /// <summary>
    /// Handles domain notifications related to task assignments and propagates them as real-time events.
    /// </summary>
    public sealed class TaskAssignmentChangedHandler(IRealtimeNotifier notifier) :
        INotificationHandler<TaskAssignmentCreated>,
        INotificationHandler<TaskAssignmentUpdated>,
        INotificationHandler<TaskAssignmentRemoved>
    {
        /// <summary>Handles the creation of a new assignment by broadcasting a <c>assignment.created</c> event.</summary>
        public Task Handle(TaskAssignmentCreated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskAssignmentCreatedEvent(n.ProjectId, n.Payload),
                cancellationToken);

        /// <summary>Handles an assignment role update by broadcasting a <c>assignment.updated</c> event.</summary>
        public Task Handle(TaskAssignmentUpdated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskAssignmentUpdatedEvent(n.ProjectId, n.Payload),
                cancellationToken);

        /// <summary>Handles the removal of an assignment by broadcasting a <c>assignment.removed</c> event.</summary>
        public Task Handle(TaskAssignmentRemoved n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskAssignmentRemovedEvent(n.ProjectId, n.Payload),
                cancellationToken);
    }

}
