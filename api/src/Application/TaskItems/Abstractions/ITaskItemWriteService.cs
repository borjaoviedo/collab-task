using Application.TaskItems.DTOs;

namespace Application.TaskItems.Abstractions
{
    /// <summary>
    /// Provides write operations for managing <see cref="TaskItem"/> aggregates,
    /// including creation, editing, movement across lanes/columns, and deletion.
    /// All operations enforce task-level invariants, column/lane constraints,
    /// and project membership/authorization rules. Returned results are mapped to
    /// <see cref="TaskItemReadDto"/> to provide a stable representation for clients.
    /// </summary>
    public interface ITaskItemWriteService
    {
        /// <summary>
        /// Creates a new task inside the specified column and lane.
        /// Throws <see cref="Common.Exceptions.ConflictException"/> when a title conflict exists,
        /// or <see cref="Common.Exceptions.NotFoundException"/> when the parent lane/column does not exist.
        /// </summary>
        /// <param name="projectId">The identifier of the project where the new task is created.</param>
        /// <param name="laneId">The identifier of the lane that contains the target column.</param>
        /// <param name="columnId">The identifier of the column to which the task will be added.</param>
        /// <param name="userId">The user performing the operation (author of the task).</param>
        /// <param name="dto">The task creation data (title, description, due date, etc.).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskItemReadDto"/> representing the newly created task.
        /// </returns>
        Task<TaskItemReadDto> CreateAsync(
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid userId,
            TaskItemCreateDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Updates an existing task’s editable fields, such as title, description, and due date.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the task does not exist.
        /// </summary>
        /// <param name="projectId">The identifier of the project containing the task.</param>
        /// <param name="taskId">The identifier of the task to edit.</param>
        /// <param name="userId">The user performing the edit.</param>
        /// <param name="dto">The edit data for the task.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskItemReadDto"/> representing the updated task.
        /// </returns>
        Task<TaskItemReadDto> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            TaskItemEditDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Moves a task to a different column or lane, adjusting its internal sort key
        /// and recomputing order when required by the board.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the task does not exist
        /// or when the target column/lane is invalid.
        /// </summary>
        /// <param name="projectId">The identifier of the project containing the task.</param>
        /// <param name="taskId">The identifier of the task to move.</param>
        /// <param name="userId">The user performing the move.</param>
        /// <param name="dto">Move parameters including target lane/column.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskItemReadDto"/> representing the task after the move.
        /// </returns>
        Task<TaskItemReadDto> MoveAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            TaskItemMoveDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes an existing task.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the task does not exist,
        /// and may throw <see cref="Common.Exceptions.ConflictException"/> if board invariants prevent deletion.
        /// </summary>
        /// <param name="projectId">The identifier of the project containing the task.</param>
        /// <param name="taskId">The identifier of the task to delete.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteByIdAsync(Guid projectId, Guid taskId, CancellationToken ct = default);
    }
}
