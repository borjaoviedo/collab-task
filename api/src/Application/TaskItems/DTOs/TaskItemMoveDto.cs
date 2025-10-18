
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemMoveDto
    {
        public required Guid TargetColumnId { get; init; }
        public required Guid TargetLaneId { get; init; }
        public required decimal TargetSortKey { get; init; }
    }
}
