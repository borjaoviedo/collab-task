
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemCreateDto
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public decimal SortKey { get; set; }
    }
}
