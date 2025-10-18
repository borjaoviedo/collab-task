
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemCreateDto
    {
        public required string Title { get; init; }
        public required string Description { get; init; }
        public DateTimeOffset? DueDate { get; init; }
        public required decimal SortKey { get; init; }
    }
}
