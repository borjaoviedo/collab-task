using Application.TaskAssignments.Changes;
using Domain.Entities;
using Domain.Enums;

namespace Application.TaskAssignments.Abstractions
{
    /// <summary>
    /// Defines persistence operations for task assignment entities.
    /// </summary>
    public interface ITaskAssignmentRepository
    {
        /// <summary>Lists all assignments for a given task.</summary>
        Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(
            Guid taskId,
            CancellationToken ct = default);

        /// <summary>Lists all assignments for a given user.</summary>
        Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(
            Guid userId,
            CancellationToken ct = default);

        /// <summary>Gets an assignment by task and user identifiers without tracking.</summary>
        Task<TaskAssignment?> GetByTaskAndUserIdAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>Gets an assignment by task and user identifiers with tracking enabled.</summary>
        Task<TaskAssignment?> GetTrackedByTaskAndUserIdAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>Adds a new assignment to the persistence context.</summary>
        Task AddAsync(TaskAssignment assignment, CancellationToken ct = default);

        /// <summary>
        /// Changes the role of an assignment and returns both the precheck result and the resulting change metadata.
        /// </summary>
        Task<(PrecheckStatus Status, AssignmentChange? Change)> ChangeRoleAsync(
            Guid taskId,
            Guid userId,
            TaskRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes an assignment if concurrency checks pass.</summary>
        Task<PrecheckStatus> DeleteAsync(
            Guid taskId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Checks whether an assignment exists for the specified task and user.</summary>
        Task<bool> ExistsAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Determines whether a task currently has any owner assignments,
        /// optionally excluding one user from the check.
        /// </summary>
        Task<bool> AnyOwnerAsync(
            Guid taskId,
            Guid? excludeUserId = null,
            CancellationToken ct = default);
    }
}
