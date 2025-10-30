using Application.TaskItems.Abstractions;
using Domain.Entities;

namespace Application.TaskItems.Services
{
    /// <summary>
    /// Read-only application service for task items.
    /// </summary>
    public sealed class TaskItemReadService(ITaskItemRepository repo) : ITaskItemReadService
    {
        /// <summary>Retrieves a task by its identifier.</summary>
        public async Task<TaskItem?> GetAsync(Guid taskId, CancellationToken ct = default)
            => await repo.GetByIdAsync(taskId, ct);

        /// <summary>Lists all tasks for a given column ordered by sort key.</summary>
        public async Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default)
            => await repo.ListByColumnAsync(columnId, ct);
    }

}
