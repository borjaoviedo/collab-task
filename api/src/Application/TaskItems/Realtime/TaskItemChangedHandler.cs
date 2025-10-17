using Application.Realtime;
using MediatR;

namespace Application.TaskItems.Realtime
{
    public sealed class TaskItemChangedHandler(IBoardNotifier notifier) :
    INotificationHandler<TaskItemCreated>,
    INotificationHandler<TaskItemUpdated>,
    INotificationHandler<TaskItemMoved>
    {
        public Task Handle(TaskItemCreated n, CancellationToken cancellationToken) => notifier.NotifyAsync(n.ProjectId, new TaskCreatedEvent(n.ProjectId, n.Payload), cancellationToken);
        public Task Handle(TaskItemUpdated n, CancellationToken cancellationToken) => notifier.NotifyAsync(n.ProjectId, new TaskUpdatedEvent(n.ProjectId, n.Payload), cancellationToken);
        public Task Handle(TaskItemMoved n, CancellationToken cancellationToken) => notifier.NotifyAsync(n.ProjectId, new TaskMovedEvent(n.ProjectId, n.Payload), cancellationToken);
    }
}
