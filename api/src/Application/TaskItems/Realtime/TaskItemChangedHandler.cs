using Application.Realtime;
using MediatR;

namespace Application.TaskItems.Realtime
{
    public sealed class TaskItemChangedHandler(IBoardNotifier notifier) :
    INotificationHandler<TaskItemCreated>,
    INotificationHandler<TaskItemUpdated>,
    INotificationHandler<TaskItemMoved>,
    INotificationHandler<TaskItemDeleted>
    {
        public Task Handle(TaskItemCreated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskItemCreatedEvent(n.ProjectId, n.Payload), cancellationToken);
        public Task Handle(TaskItemUpdated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskItemUpdatedEvent(n.ProjectId, n.Payload), cancellationToken);
        public Task Handle(TaskItemMoved n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskItemMovedEvent(n.ProjectId, n.Payload), cancellationToken);
        public Task Handle(TaskItemDeleted n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskItemDeletedEvent(n.ProjectId, n.Payload), cancellationToken);
    }
}
