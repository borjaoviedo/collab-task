
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemMoveDto
    {
        public required Guid TargetColumnId { get; set; }
        public required Guid TargetLaneId { get; set; }
        public required decimal TargetSortKey { get; set; }
    }
}
