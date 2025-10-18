
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemEditDto
    {
        public string? NewTitle { get; set; } = default!;
        public string? NewDescription { get; set; } = default!;
        public DateTimeOffset? NewDueDate { get; set; }
    }
}
