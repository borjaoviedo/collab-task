namespace Application.TaskItems.Changes
{
    public enum TaskItemChangeKind { Edited, Moved }

    public sealed record TaskItemEditedChange(
        string? OldTitle, string? NewTitle,
        string? OldDescription, string? NewDescription,
        DateTimeOffset? OldDueDate, DateTimeOffset? NewDueDate
    ) : TaskItemChange(TaskItemChangeKind.Edited);

    public sealed record TaskItemMovedChange(
        Guid FromLaneId, Guid ToLaneId,
        Guid FromColumnId, Guid ToColumnId,
        decimal FromSortKey, decimal ToSortKey
    ) : TaskItemChange(TaskItemChangeKind.Moved);

    public abstract record TaskItemChange(TaskItemChangeKind Kind);
}
