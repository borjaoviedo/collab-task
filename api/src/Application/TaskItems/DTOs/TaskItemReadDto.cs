
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemReadDto
    {
        public Guid Id { get; init; }
        public Guid ColumnId { get; init; }
        public Guid LaneId { get; init; }
        public Guid ProjectId { get; init; }
        public string Title { get; init; } = default!;
        public string Description { get; init; } = default!;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public DateTimeOffset? DueDate { get; init; }
        public decimal SortKey { get; init; }
        public byte[] RowVersion { get; init; } = default!;
    }
}
