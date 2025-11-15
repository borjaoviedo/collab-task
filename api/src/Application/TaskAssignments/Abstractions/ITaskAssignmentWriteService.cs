using Application.TaskAssignments.DTOs;

namespace Application.TaskAssignments.Abstractions
{
    /// <summary>
    /// Provides write operations for managing <see cref="Domain.Entities.TaskAssignment"/> entities,
    /// including creation, role changes, and deletion of assignments. These operations
    /// enforce task-level ownership rules, assignment invariants, and authorization
    /// semantics by requiring the identifier of the user executing the action.
    /// All returned assignment states are mapped to <see cref="TaskAssignmentReadDto"/>
    /// to provide a stable client-facing representation.
    /// </summary>
    public interface ITaskAssignmentWriteService
    {
        /// <summary>
        /// Creates a new task assignment for the specified user within the given task.
        /// Throws <see cref="Common.Exceptions.ConflictException"/> when an assignment already exists
        /// or when task-level ownership constraints prevent creation.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project containing the task.</param>
        /// <param name="taskId">The unique identifier of the task to which the assignment belongs.</param>
        /// <param name="executedBy">The user performing the assignment operation.</param>
        /// <param name="dto">The data required to create the task assignment.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskAssignmentReadDto"/> describing the newly created assignment.
        /// </returns>
        Task<TaskAssignmentReadDto> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid executedBy,
            TaskAssignmentCreateDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Updates the role of an existing task assignment.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the assignment does not exist
        /// and <see cref="Common.Exceptions.ConflictException"/> when role constraints are violated.
        /// </summary>
        /// <param name="projectId">The identifier of the project containing the task.</param>
        /// <param name="taskId">The identifier of the task to which the assignment belongs.</param>
        /// <param name="targetUserId">The user whose assignment role will be changed.</param>
        /// <param name="executedBy">The identifier of the user performing the change.</param>
        /// <param name="dto">The new role to apply to the assignment.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskAssignmentReadDto"/> representing the updated assignment.
        /// </returns>
        Task<TaskAssignmentReadDto> ChangeRoleAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            Guid executedBy,
            TaskAssignmentChangeRoleDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes an existing task assignment.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the assignment does not exist
        /// and <see cref="Common.Exceptions.ConflictException"/> when deletion violates ownership rules
        /// (for example, removing the last task owner).
        /// </summary>
        /// <param name="projectId">The identifier of the project containing the task.</param>
        /// <param name="taskId">The identifier of the task whose assignment will be removed.</param>
        /// <param name="targetUserId">The user whose assignment will be deleted.</param>
        /// <param name="executedBy">The identifier of the user initiating the deletion.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            Guid executedBy,
            CancellationToken ct = default);
    }
}
