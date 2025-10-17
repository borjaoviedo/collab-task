using Domain.Entities;
using Domain.Enums;

namespace Application.TaskAssignments.Abstractions
{
    public interface ITaskAssignmentWriteService
    {
        Task<(DomainMutation, TaskAssignment?)> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid affectedUserId,
            TaskRole role,
            Guid performedBy,
            CancellationToken ct = default);
        Task<DomainMutation> AssignAsync(
            Guid projectId,
            Guid taskId,
            Guid affectedUserId,
            TaskRole role,
            Guid performedBy,
            CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(
            Guid projectId,
            Guid taskId,
            Guid affectedUserId,
            TaskRole newRole,
            Guid performedBy,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<DomainMutation> RemoveAsync(
            Guid projectId,
            Guid taskId,
            Guid affectedUserId,
            Guid performedBy,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
