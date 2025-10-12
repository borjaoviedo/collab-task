
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemCreateDto
    {
        public Guid ColumnId { get; set; }
        public Guid LaneId { get; set; }
        public Guid ProjectId { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public decimal SortKey { get; set; }
    }
}
