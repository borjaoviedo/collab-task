using Domain.Entities;

namespace Application.TaskAssignments.Abstractions
{
    public interface ITaskAssignmentReadService
    {
        Task<TaskAssignment?> GetAsync(Guid taskId, Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    }
}
