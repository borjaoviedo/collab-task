using Application.TaskAssignments.Abstractions;
using Domain.Entities;

namespace Application.TaskAssignments.Services
{
    public sealed class TaskAssignmentReadService(ITaskAssignmentRepository repo) : ITaskAssignmentReadService
    {
        public async Task<TaskAssignment?> GetAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await repo.GetAsync(taskId, userId, ct);

        public async Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await repo.ListByTaskAsync(taskId, ct);

        public async Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await repo.ListByUserAsync(userId, ct);
    }
}
