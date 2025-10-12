
namespace Application.TaskNotes.DTOs
{
    public sealed class TaskNoteEditDto
    {
        public Guid Id { get; set; }
        public required string Content { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
