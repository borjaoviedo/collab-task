using Domain.Entities;

namespace Application.TaskItems.Abstractions
{
    /// <summary>
    /// Defines persistence operations for <see cref="TaskItem"/> entities,
    /// including retrieval by column, tracked and untracked lookup, title-uniqueness
    /// checks, ordering utilities, and CRUD operations. Task items represent work units
    /// within a column of the project board, and this repository provides the low-level
    /// querying primitives required to support creation, movement, editing, and deletion
    /// operations while preserving domain invariants such as unique titles per column
    /// and stable ordering via <c>SortKey</c>.
    /// </summary>
    public interface ITaskItemRepository
    {
        /// <summary>
        /// Retrieves all task items belonging to the specified column, typically
        /// ordered by their <c>SortKey</c> or similar ordering attribute by the caller.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column whose tasks will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskItem"/> entities associated with the given column.
        /// </returns>
        Task<IReadOnlyList<TaskItem>> ListByColumnIdAsync(
            Guid columnId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a task item by its unique identifier without enabling EF Core tracking.
        /// Useful for read-only scenarios and API queries where mutation is not required.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="TaskItem"/> entity, or <c>null</c> if no matching task is found.
        /// </returns>
        Task<TaskItem?> GetByIdAsync(
            Guid taskId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a task item by its unique identifier with EF Core tracking enabled.
        /// Use this when planning to mutate the entity so EF Core can detect changed values
        /// and generate minimal UPDATE statements.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The tracked <see cref="TaskItem"/> entity, or <c>null</c> if no matching task exists.
        /// </returns>
        Task<TaskItem?> GetByIdForUpdateAsync(
            Guid taskId,
            CancellationToken ct = default);

        /// <summary>
        /// Determines whether a task with the specified title already exists within the
        /// given column. An optional <paramref name="excludeTaskId"/> may be provided to
        /// exclude a specific task from the check (useful when renaming).
        /// </summary>
        /// <param name="columnId">The column under which the title check is performed.</param>
        /// <param name="taskTitle">The task title to validate for uniqueness.</param>
        /// <param name="excludeTaskId">An optional task identifier to exclude from the check.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// <c>true</c> if another task in the column already uses the given title; otherwise <c>false</c>.
        /// </returns>
        Task<bool> ExistsWithTitleAsync(
            Guid columnId,
            string taskTitle,
            Guid? excludeTaskId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves the next <c>SortKey</c> value for tasks within the specified column.
        /// Used to append new tasks at the end of the ordering sequence while guaranteeing
        /// stable ordering semantics. Implementations typically compute this based on the
        /// maximum existing sort key.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column whose next sort key is requested.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A numeric sort key suitable for placing a new task at the end of the ordering.
        /// </returns>
        Task<decimal> GetNextSortKeyAsync(
            Guid columnId,
            CancellationToken ct = default);

        /// <summary>
        /// Adds a new <see cref="TaskItem"/> entity to the persistence context.
        /// </summary>
        /// <param name="task">The task entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(TaskItem task, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing <see cref="TaskItem"/> within the persistence context.
        /// EF Core change tracking will detect modifications and generate minimal update statements.
        /// </summary>
        /// <param name="task">The task entity with updated state.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task UpdateAsync(TaskItem task, CancellationToken ct = default);

        /// <summary>
        /// Removes a task item from the persistence context.
        /// Actual deletion occurs during the persistence cycle of the surrounding unit of work.
        /// </summary>
        /// <param name="task">The task entity to remove.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task RemoveAsync(TaskItem task, CancellationToken ct = default);
    }
}
