using Domain.Entities;
using Domain.Enums;

namespace Application.TaskAssignments.Abstractions
{
    public interface ITaskAssignmentRepository
    {
        Task<TaskAssignment?> GetAsync(Guid taskId, Guid userId, CancellationToken ct = default);
        Task<TaskAssignment?> GetTrackedAsync(Guid taskId, Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid taskId, Guid userId, CancellationToken ct = default);
        Task<bool> AnyOwnerAsync(Guid taskId, CancellationToken ct = default);
        Task<int> CountByRoleAsync(Guid taskId, TaskRole role, CancellationToken ct = default);

        Task AddAsync(TaskAssignment assignment, CancellationToken ct = default);
        Task<DomainMutation> AssignAsync(Guid taskId, Guid userId, TaskRole role, CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(Guid taskId, Guid userId, TaskRole newRole, CancellationToken ct = default);
        Task<DomainMutation> RemoveAsync(Guid taskId, Guid userId, CancellationToken ct = default);

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
