using Domain.Entities;
using Domain.Enums;

namespace Application.TaskAssignments.Abstractions
{
    public interface ITaskAssignmentWriteService
    {
        Task<(DomainMutation, TaskAssignment?)> CreateAsync(
            Guid taskId,
            Guid affectedUserId,
            TaskRole role,
            Guid performedBy,
            CancellationToken ct = default);
        Task<DomainMutation> AssignAsync(
            Guid taskId,
            Guid affectedUserId,
            TaskRole role,
            Guid performedBy,
            CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(
            Guid taskId,
            Guid affectedUserId,
            TaskRole newRole,
            Guid performedBy,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<DomainMutation> RemoveAsync(
            Guid taskId,
            Guid affectedUserId,
            Guid performedBy,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
