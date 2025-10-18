
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemEditDto
    {
        public string? NewTitle { get; init; } = default!;
        public string? NewDescription { get; init; } = default!;
        public DateTimeOffset? NewDueDate { get; init; }
    }
}
