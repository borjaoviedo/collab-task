using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Abstractions
{
    public interface ITaskActivityRepository
    {
        Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskActivity>> ListByActorAsync(Guid actorId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(Guid taskId, TaskActivityType type, CancellationToken ct = default);

        Task AddAsync(TaskActivity activity, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<TaskActivity> activities, CancellationToken ct = default);

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
