using Domain.Entities;
using Domain.Enums;

namespace Application.TaskAssignments.Abstractions
{
    public interface ITaskAssignmentWriteService
    {
        Task<(DomainMutation, TaskAssignment?)> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole role,
            Guid executedBy,
            CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole newRole,
            Guid executedBy,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            Guid executedBy,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
