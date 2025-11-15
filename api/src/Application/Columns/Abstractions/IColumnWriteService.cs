using Application.Columns.DTOs;
using Domain.Entities;

namespace Application.Columns.Abstractions
{
    /// <summary>
    /// Provides write operations for managing <see cref="Column"/> entities,
    /// including creation, renaming, reordering, and deletion within a lane.
    /// All operations enforce domain invariants, naming uniqueness,
    /// board-structure integrity, and optimistic concurrency where applicable.
    /// </summary>
    public interface IColumnWriteService
    {
        /// <summary>
        /// Creates a new column within the specified lane of a project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the parent project.</param>
        /// <param name="laneId">The unique identifier of the parent lane.</param>
        /// <param name="dto">The data required to create the column.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ColumnReadDto"/> describing the newly created column.
        /// </returns>
        Task<ColumnReadDto> CreateAsync(
            Guid projectId,
            Guid laneId,
            ColumnCreateDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Renames an existing column.
        /// Throws <see cref="Common.Exceptions.ConflictException"/> when a name conflict occurs
        /// or when concurrency validation fails.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column to rename.</param>
        /// <param name="dto">The new name and associated metadata.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ColumnReadDto"/> representing the updated column.
        /// </returns>
        Task<ColumnReadDto> RenameAsync(
            Guid columnId,
            ColumnRenameDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Updates the display order of a column within its lane.
        /// Performs validation on position constraints and lane consistency
        /// before applying changes.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column to reorder.</param>
        /// <param name="dto">The new ordering information.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ColumnReadDto"/> representing the reordered column.
        /// </returns>
        Task<ColumnReadDto> ReorderAsync(
            Guid columnId,
            ColumnReorderDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes a column from its lane.
        /// Enforces concurrency checks and validates that board structure
        /// remains consistent after deletion.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column to delete.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteByIdAsync(
            Guid columnId,
            CancellationToken ct = default);
    }
}
