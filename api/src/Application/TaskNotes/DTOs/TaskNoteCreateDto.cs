
namespace Application.TaskNotes.DTOs
{
    public sealed class TaskNoteCreateDto
    {
        public Guid TaskId { get; set; }
        public Guid AuthorId { get; set; }
        public string Content { get; set; } = default!;
    }
}
