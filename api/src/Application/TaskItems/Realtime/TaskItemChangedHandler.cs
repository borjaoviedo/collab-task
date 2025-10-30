using Application.Realtime;
using MediatR;

namespace Application.TaskItems.Realtime
{
    /// <summary>
    /// Handles domain notifications related to task items and propagates them as real-time events.
    /// </summary>
    public sealed class TaskItemChangedHandler(IRealtimeNotifier notifier) :
        INotificationHandler<TaskItemCreated>,
        INotificationHandler<TaskItemUpdated>,
        INotificationHandler<TaskItemMoved>,
        INotificationHandler<TaskItemDeleted>
    {
        /// <summary>Broadcasts a <c>task.created</c> event when a task item is created.</summary>
        public Task Handle(TaskItemCreated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskItemCreatedEvent(n.ProjectId, n.Payload),
                cancellationToken);

        /// <summary>Broadcasts a <c>task.updated</c> event when a task item is updated.</summary>
        public Task Handle(TaskItemUpdated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskItemUpdatedEvent(n.ProjectId, n.Payload),
                cancellationToken);

        /// <summary>Broadcasts a <c>task.moved</c> event when a task item is moved.</summary>
        public Task Handle(TaskItemMoved n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskItemMovedEvent(n.ProjectId, n.Payload),
                cancellationToken);

        /// <summary>Broadcasts a <c>task.deleted</c> event when a task item is deleted.</summary>
        public Task Handle(TaskItemDeleted n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskItemDeletedEvent(n.ProjectId, n.Payload),
                cancellationToken);
    }

}
