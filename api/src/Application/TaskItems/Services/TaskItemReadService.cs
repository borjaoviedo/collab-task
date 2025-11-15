using Application.Common.Exceptions;
using Application.TaskItems.Abstractions;
using Application.TaskItems.DTOs;
using Application.TaskItems.Mapping;

namespace Application.TaskItems.Services
{
    /// <summary>
    /// Application read-side service for <see cref="Domain.Entities.TaskItem"/> aggregates.
    /// Provides high-level query operations for retrieving a single task item or listing all
    /// task items belonging to a specific column. All retrieved entities are mapped to
    /// <see cref="TaskItemReadDto"/> to expose a stable, API-oriented representation.
    /// Missing tasks are surfaced as <see cref="NotFoundException"/> to ensure
    /// consistent error behavior across the application.
    /// </summary>
    /// <param name="taskItemRepository">
    /// Repository used for querying <see cref="Domain.Entities.TaskItem"/> entities,
    /// including lookups by identifier and lists by column.
    /// </param>
    public sealed class TaskItemReadService(
        ITaskItemRepository taskItemRepository) : ITaskItemReadService
    {
        private readonly ITaskItemRepository _taskItemRepository = taskItemRepository;

        /// <inheritdoc/>
        public async Task<TaskItemReadDto> GetByIdAsync(
            Guid taskId,
            CancellationToken ct = default)
        {
            var taskItem = await _taskItemRepository.GetByIdAsync(taskId, ct)
                // 404 if the task does not exist
                ?? throw new NotFoundException("Task item not found.");

            return taskItem.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskItemReadDto>> ListByColumnIdAsync(
            Guid columnId,
            CancellationToken ct = default)
        {
            var taskItems = await _taskItemRepository.ListByColumnIdAsync(columnId, ct);

            return taskItems
                .Select(ti => ti.ToReadDto())
                .ToList();
        }
    }
}
