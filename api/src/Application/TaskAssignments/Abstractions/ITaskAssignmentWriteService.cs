using Domain.Entities;
using Domain.Enums;

namespace Application.TaskAssignments.Abstractions
{
    /// <summary>
    /// Handles task assignment commands at the application layer.
    /// </summary>
    public interface ITaskAssignmentWriteService
    {
        /// <summary>Creates a new task assignment for a target user.</summary>
        Task<(DomainMutation, TaskAssignment?)> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole role,
            Guid executedBy,
            CancellationToken ct = default);

        /// <summary>Changes the role of an existing task assignment.</summary>
        Task<DomainMutation> ChangeRoleAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole newRole,
            Guid executedBy,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes an existing task assignment.</summary>
        Task<DomainMutation> DeleteAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            Guid executedBy,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
