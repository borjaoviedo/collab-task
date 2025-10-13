using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Services
{
    public sealed class TaskActivityReadService(ITaskActivityRepository repo) : ITaskActivityReadService
    {
        public async Task<TaskActivity?> GetAsync(Guid activityId, CancellationToken ct = default)
            => await repo.GetByIdAsync(activityId, ct);

        public async Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await repo.ListByTaskAsync(taskId, ct);

        public async Task<IReadOnlyList<TaskActivity>> ListByActorAsync(Guid actorId, CancellationToken ct = default)
            => await repo.ListByActorAsync(actorId, ct);

        public async Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(Guid taskId, TaskActivityType type, CancellationToken ct = default)
            => await repo.ListByTypeAsync(taskId, type, ct);
    }
}
