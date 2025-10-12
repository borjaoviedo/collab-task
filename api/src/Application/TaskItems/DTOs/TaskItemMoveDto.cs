
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemMoveDto
    {
        public Guid Id { get; set; }
        public Guid TargetColumnId { get; set; }
        public Guid TargetLaneId { get; set; }
        public decimal TargetSortKey { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
