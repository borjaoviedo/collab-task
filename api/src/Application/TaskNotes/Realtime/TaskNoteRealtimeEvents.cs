
namespace Application.TaskNotes.Realtime
{
    public sealed record TaskNoteCreatedPayload(Guid TaskId, Guid NoteId, string Content);
    public sealed record TaskNoteUpdatedPayload(Guid NoteId, string NewContent);
    public sealed record TaskNoteDeletedPayload(Guid NoteId);

    public sealed record TaskNoteCreatedEvent(Guid ProjectId, TaskNoteCreatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskNoteCreatedPayload>("note.created", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskNoteUpdatedEvent(Guid ProjectId, TaskNoteUpdatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskNoteUpdatedPayload>("note.updated", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskNoteDeletedEvent(Guid ProjectId, TaskNoteDeletedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskNoteDeletedPayload>("note.deleted", ProjectId, DateTimeOffset.UtcNow, Payload);
}
