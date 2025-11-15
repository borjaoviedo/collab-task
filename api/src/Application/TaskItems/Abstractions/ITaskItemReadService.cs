using Application.TaskItems.DTOs;

namespace Application.TaskItems.Abstractions
{
    /// <summary>
    /// Provides read-only access to <see cref="TaskItem"/> entities.
    /// Exposes query operations for retrieving a single task item or listing
    /// all task items within a specific column. Returned results are projected
    /// to <see cref="TaskItemReadDto"/> to provide a stable, API-facing read model
    /// used by task boards, UI renderers, and application-layer workflows.
    /// </summary>
    public interface ITaskItemReadService
    {
        /// <summary>
        /// Retrieves a task item by its unique identifier.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the task does not exist.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task to retrieve.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskItemReadDto"/> describing the task item.
        /// </returns>
        Task<TaskItemReadDto> GetByIdAsync(
            Guid taskId,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all task items associated with the specified column.
        /// Results are typically ordered by <c>SortKey</c> or another display ordering
        /// defined in the domain model.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column whose tasks will be retrieved.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskItemReadDto"/> entries belonging to the column.
        /// </returns>
        Task<IReadOnlyList<TaskItemReadDto>> ListByColumnIdAsync(
            Guid columnId,
            CancellationToken ct = default);
    }
}
