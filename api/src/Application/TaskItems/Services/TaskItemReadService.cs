using Application.TaskItems.Abstractions;
using Domain.Entities;

namespace Application.TaskItems.Services
{
    public sealed class TaskItemReadService(ITaskItemRepository repo) : ITaskItemReadService
    {
        public async Task<TaskItem?> GetAsync(Guid taskId, CancellationToken ct = default)
            => await repo.GetByIdAsync(taskId, ct);

        public async Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default)
            => await repo.ListByColumnAsync(columnId, ct);
    }
}
