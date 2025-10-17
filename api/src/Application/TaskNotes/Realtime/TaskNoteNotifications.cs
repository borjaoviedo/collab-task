using MediatR;

namespace Application.TaskNotes.Realtime
{
    public sealed record TaskNoteCreated(Guid ProjectId, TaskNoteCreatedPayload Payload) : INotification;
    public sealed record TaskNoteUpdated(Guid ProjectId, TaskNoteUpdatedPayload Payload) : INotification;
    public sealed record TaskNoteDeleted(Guid ProjectId, TaskNoteDeletedPayload Payload) : INotification;
}
