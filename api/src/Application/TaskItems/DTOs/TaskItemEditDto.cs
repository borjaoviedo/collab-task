
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemEditDto
    {
        public string? Title { get; set; } = default!;
        public string? Description { get; set; } = default!;
        public DateTimeOffset? DueDate { get; set; }
    }
}
