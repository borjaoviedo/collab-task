using Domain.Entities;

namespace Application.TaskItems.Abstractions
{
    public interface ITaskItemReadService
    {
        Task<TaskItem?> GetAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default);
    }
}
