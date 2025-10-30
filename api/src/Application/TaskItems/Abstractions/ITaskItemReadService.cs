using Domain.Entities;

namespace Application.TaskItems.Abstractions
{
    /// <summary>
    /// Provides read-only access to task items within columns.
    /// </summary>
    public interface ITaskItemReadService
    {
        /// <summary>Retrieves a task by its unique identifier.</summary>
        Task<TaskItem?> GetAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>Lists all tasks contained in a specific column.</summary>
        Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default);
    }
}
