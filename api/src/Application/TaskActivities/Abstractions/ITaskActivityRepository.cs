using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Abstractions
{
    /// <summary>
    /// Defines persistence operations for <see cref="TaskActivity"/> entities,
    /// including retrieval by task, user, or activity type, single-activity lookup,
    /// and creation of new activity records. These operations support
    /// audit-history views, task timelines, and real-time update scenarios
    /// within the application.
    /// </summary>
    public interface ITaskActivityRepository
    {
        /// <summary>
        /// Retrieves all activity records associated with a specific task.
        /// Activities are typically ordered chronologically by the caller.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task whose activities will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskActivity"/> entries for the specified task.
        /// </returns>
        Task<IReadOnlyList<TaskActivity>> ListByTaskIdAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves all activity records performed by a specific user across all tasks.
        /// Useful for generating user-centric audit feeds or recent activity dashboards.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose activity will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskActivity"/> entries produced by the specified user.
        /// </returns>
        Task<IReadOnlyList<TaskActivity>> ListByUserIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves activity entries of a specific type for a given task.
        /// Useful for filtering timeline events (e.g., assignments, notes, moves)
        /// or querying only relevant activity categories.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task whose activities will be retrieved.</param>
        /// <param name="type">The activity type to filter by.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskActivity"/> entries matching the given task and type.
        /// </returns>
        Task<IReadOnlyList<TaskActivity>> ListByTaskTypeAsync(
            Guid taskId,
            TaskActivityType type,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a task activity entry by its unique identifier.
        /// </summary>
        /// <param name="activityId">The unique identifier of the activity to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="TaskActivity"/> entity, or <c>null</c> if no matching record is found.
        /// </returns>
        Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default);

        /// <summary>
        /// Adds a new task activity entry to the persistence context.
        /// Used by the application layer to record actions performed on tasks,
        /// such as creation, updates, assignments, notes, or movements.
        /// </summary>
        /// <param name="activity">The activity entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(TaskActivity activity, CancellationToken ct = default);
    }
}
