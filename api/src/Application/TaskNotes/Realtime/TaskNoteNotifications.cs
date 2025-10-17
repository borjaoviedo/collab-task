using MediatR;

namespace Application.TaskNotes.Realtime
{
    public sealed record TaskNoteItemCreated(Guid ProjectId, TaskNoteCreatedPayload Payload) : INotification;
}
