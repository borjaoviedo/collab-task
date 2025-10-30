
namespace Application.TaskNotes.Realtime
{
    public sealed record TaskNoteCreatedPayload(Guid TaskId, Guid NoteId, string Content);
    public sealed record TaskNoteUpdatedPayload(Guid TaskId, Guid NoteId, string NewContent);
    public sealed record TaskNoteDeletedPayload(Guid TaskId, Guid NoteId);

    /// <summary>Event emitted when a task note is created.</summary>
    public sealed record TaskNoteCreatedEvent(Guid ProjectId, TaskNoteCreatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskNoteCreatedPayload>(
            TypeName,
            ProjectId,
            DateTimeOffset.UtcNow,
            Payload)
    {
        public const string TypeName = "note.created";
    }

    /// <summary>Event emitted when a task note is updated.</summary>
    public sealed record TaskNoteUpdatedEvent(Guid ProjectId, TaskNoteUpdatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskNoteUpdatedPayload>(
            TypeName,
            ProjectId,
            DateTimeOffset.UtcNow,
            Payload)
    {
        public const string TypeName = "note.updated";
    }

    /// <summary>Event emitted when a task note is deleted.</summary>
    public sealed record TaskNoteDeletedEvent(Guid ProjectId, TaskNoteDeletedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskNoteDeletedPayload>(
            TypeName,
            ProjectId,
            DateTimeOffset.UtcNow,
            Payload)
    {
        public const string TypeName = "note.deleted";
    }
}
