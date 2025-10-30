using Application.TaskItems.Changes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskItems.Abstractions
{
    /// <summary>
    /// Defines persistence operations for task item entities.
    /// </summary>
    public interface ITaskItemRepository
    {
        /// <summary>Lists all tasks of a given column ordered by sort key.</summary>
        Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default);

        /// <summary>Gets a task by its identifier without tracking.</summary>
        Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>Gets a task by its identifier with tracking enabled.</summary>
        Task<TaskItem?> GetTrackedByIdAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>Adds a new task to the persistence context.</summary>
        Task AddAsync(TaskItem task, CancellationToken ct = default);

        /// <summary>
        /// Edits a task and returns both the precheck result and a change descriptor when successful.
        /// </summary>
        Task<(PrecheckStatus Status, TaskItemEditedChange? Change)> EditAsync(
            Guid taskId,
            TaskTitle? newTitle,
            TaskDescription? newDescription,
            DateTimeOffset? newDueDate,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Moves a task between columns and lanes and returns metadata describing the movement.
        /// </summary>
        Task<(PrecheckStatus Status, TaskItemMovedChange? Change)> MoveAsync(
            Guid taskId,
            Guid targetColumnId,
            Guid targetLaneId,
            Guid targetProjectId,
            decimal targetSortKey,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes a task if concurrency checks pass.</summary>
        Task<PrecheckStatus> DeleteAsync(
            Guid taskId,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Checks whether a task with the same title already exists in the given column.
        /// </summary>
        Task<bool> ExistsWithTitleAsync(
            Guid columnId,
            TaskTitle title,
            Guid? excludeTaskId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Gets the next available sort key value for ordering within a column.
        /// </summary>
        Task<decimal> GetNextSortKeyAsync(Guid columnId, CancellationToken ct = default);
    }
}
