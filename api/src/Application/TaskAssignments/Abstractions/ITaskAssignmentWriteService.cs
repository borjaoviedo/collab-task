using Domain.Entities;
using Domain.Enums;

namespace Application.TaskAssignments.Abstractions
{
    public interface ITaskAssignmentWriteService
    {
        Task<(DomainMutation, TaskAssignment?)> CreateAsync(Guid taskId, Guid userId, TaskRole role, CancellationToken ct = default);
        Task<DomainMutation> AssignAsync(Guid taskId, Guid userId, TaskRole role, CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(Guid taskId, Guid userId, TaskRole newRole, CancellationToken ct = default);
        Task<DomainMutation> RemoveAsync(Guid taskId, Guid userId, CancellationToken ct = default);
    }
}
