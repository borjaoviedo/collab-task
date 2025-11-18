using Application.TaskAssignments.DTOs;

namespace Application.TaskAssignments.Abstractions
{
    /// <summary>
    /// Provides read-only access to <see cref="TaskAssignment"/> entities,
    /// exposing query operations for retrieving individual task–user assignments
    /// as well as listing assignments by task or by user. This service returns
    /// <see cref="TaskAssignmentReadDto"/> representations to ensure a stable,
    /// API-friendly read model used by higher-level features such as assignment
    /// summaries, ownership checks, and user-specific dashboards.
    /// </summary>
    public interface ITaskAssignmentReadService
    {
        /// <summary>
        /// Retrieves the assignment record for a specific task–user pair.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the assignment does not exist.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskAssignmentReadDto"/> describing the assignment.
        /// </returns>
        Task<TaskAssignmentReadDto> GetByTaskAndUserIdAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all assignment records associated with a specific task.
        /// Useful for determining owners, co-owners, and collaborators.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task whose assignments will be retrieved.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskAssignmentReadDto"/> entries belonging to the task.
        /// </returns>
        Task<IReadOnlyList<TaskAssignmentReadDto>> ListByTaskIdAsync(
            Guid taskId,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all assignment records associated with a specific user across all tasks.
        /// Supports user-centric views such as task responsibility dashboards.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose assignments will be retrieved.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskAssignmentReadDto"/> entries linked to the specified user.
        /// </returns>
        Task<IReadOnlyList<TaskAssignmentReadDto>> ListByUserIdAsync(
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all assignment records associated with the authenticated user across all tasks.
        /// Supports user-centric views such as task responsibility dashboards.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskAssignmentReadDto"/> entries linked to the authenticated user.
        /// </returns>
        Task<IReadOnlyList<TaskAssignmentReadDto>> ListSelfAsync(CancellationToken ct = default);
    }
}
