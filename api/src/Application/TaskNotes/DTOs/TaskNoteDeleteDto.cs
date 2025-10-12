
namespace Application.TaskNotes.DTOs
{
    public sealed class TaskNoteDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
