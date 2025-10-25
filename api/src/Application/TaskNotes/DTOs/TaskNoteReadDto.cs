
namespace Application.TaskNotes.DTOs
{
    public sealed class TaskNoteReadDto
    {
        public Guid Id { get; init; }
        public Guid TaskId { get; init; }
        public Guid UserId { get; init; }
        public string Content { get; init; } = default!;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public byte[] RowVersion { get; init; } = default!;
    }
}
