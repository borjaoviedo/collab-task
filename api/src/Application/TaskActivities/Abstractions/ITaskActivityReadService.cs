using Application.TaskActivities.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Abstractions
{
    /// <summary>
    /// Provides read-only access to <see cref="TaskActivity"/> entities.
    /// Exposes query operations for retrieving individual activity records,
    /// listing activities for a specific task, retrieving activity logs authored
    /// by a given user, and filtering activities by type. These operations are used
    /// to support task timelines, auditing, and real-time activity feeds.
    /// </summary>
    public interface ITaskActivityReadService
    {
        /// <summary>
        /// Retrieves a task activity by its unique identifier.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/>
        /// when the activity does not exist or is not accessible to the current user.
        /// </summary>
        /// <param name="activityId">The unique identifier of the activity to retrieve.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskActivityReadDto"/> representing the activity entry.
        /// </returns>
        Task<TaskActivityReadDto> GetByIdAsync(
            Guid activityId,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all activities related to a specific task.
        /// Results are typically ordered chronologically by the caller to form
        /// a complete task activity timeline.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task whose activity history will be retrieved.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskActivityReadDto"/> entries associated with the task.
        /// </returns>
        Task<IReadOnlyList<TaskActivityReadDto>> ListByTaskIdAsync(
            Guid taskId,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all activities performed by a specific user across all tasks.
        /// Useful for personal activity feeds, audit reports, or user-centric dashboards.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose activity entries will be retrieved.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskActivityReadDto"/> entries authored by the specified user.
        /// </returns>
        Task<IReadOnlyList<TaskActivityReadDto>> ListByUserIdAsync(
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all activities of a given <see cref="TaskActivityType"/> for a specific task.
        /// Useful for filtering task history into subsets (for example: notes, assignments, movements).
        /// </summary>
        /// <param name="taskId">The unique identifier of the task whose filtered activity entries will be retrieved.</param>
        /// <param name="type">The type of activity to filter by.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskActivityReadDto"/> entries matching the specified type.
        /// </returns>
        Task<IReadOnlyList<TaskActivityReadDto>> ListByActivityTypeAsync(
            Guid taskId,
            TaskActivityType type,
            CancellationToken ct = default);
    }
}
