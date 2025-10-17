
namespace Application.TaskNotes.Realtime
{
    public sealed record TaskNoteCreatedPayload(Guid TaskId, Guid NoteId, string Content, Guid AuthorId, DateTimeOffset CreatedAt);

    public sealed record TaskNoteCreatedEvent(Guid ProjectId, TaskNoteCreatedPayload Payload)
        : Application.Realtime.BoardEvent<TaskNoteCreatedPayload>(
            "tasknote.created",
            ProjectId,
            DateTimeOffset.UtcNow,
            Payload);
}
