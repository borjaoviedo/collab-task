using Application.Realtime;
using MediatR;

namespace Application.TaskItems.Handlers
{
    public sealed record TaskItemCreated(Guid ProjectId, TaskCreatedPayload Payload) : INotification;
    public sealed record TaskItemEdited(Guid ProjectId, TaskEditedPayload Payload) : INotification;
    public sealed record TaskItemMoved(Guid ProjectId, TaskMovedPayload Payload) : INotification;

    public sealed class TaskItemChangedHandler(IBoardNotifier notifier) :
        INotificationHandler<TaskItemCreated>,
        INotificationHandler<TaskItemEdited>,
        INotificationHandler<TaskItemMoved>
    {
        public Task Handle(TaskItemCreated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskCreatedEvent(n.ProjectId, n.Payload), cancellationToken);

        public Task Handle(TaskItemEdited n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskEditedEvent(n.ProjectId, n.Payload), cancellationToken);

        public Task Handle(TaskItemMoved n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskMovedEvent(n.ProjectId, n.Payload), cancellationToken);
    }
}
