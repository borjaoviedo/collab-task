using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Abstractions
{
    public interface ITaskActivityRepository
    {
        Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskActivity>> ListByUserAsync(Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(Guid taskId, TaskActivityType type, CancellationToken ct = default);
        Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default);

        Task AddAsync(TaskActivity activity, CancellationToken ct = default);
    }
}
