
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemMoveDto
    {
        public required Guid NewColumnId { get; init; }
        public required Guid NewLaneId { get; init; }
        public required decimal NewSortKey { get; init; }
    }
}
