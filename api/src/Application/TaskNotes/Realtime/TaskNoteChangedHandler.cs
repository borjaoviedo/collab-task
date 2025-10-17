using Application.Realtime;
using MediatR;

namespace Application.TaskNotes.Realtime
{
    public sealed class TaskNoteChangedHandler(IBoardNotifier notifier)
    : INotificationHandler<TaskNoteCreated>,
      INotificationHandler<TaskNoteUpdated>,
      INotificationHandler<TaskNoteDeleted>
    {
        public Task Handle(TaskNoteCreated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskNoteCreatedEvent(n.ProjectId, n.Payload), cancellationToken);

        public Task Handle(TaskNoteUpdated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskNoteUpdatedEvent(n.ProjectId, n.Payload), cancellationToken);

        public Task Handle(TaskNoteDeleted n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskNoteDeletedEvent(n.ProjectId, n.Payload), cancellationToken);
    }
}
