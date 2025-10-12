
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemMoveDto
    {
        public Guid Id { get; set; }
        public Guid ColumnId { get; set; }
        public Guid LaneId { get; set; }
        public decimal SortKey { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
