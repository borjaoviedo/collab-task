
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemReadDto
    {
        public Guid Id { get; set; }
        public Guid ColumnId { get; set; }
        public Guid LaneId { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public decimal SortKey { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
