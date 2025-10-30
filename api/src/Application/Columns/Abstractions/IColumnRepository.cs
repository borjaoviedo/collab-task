using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Abstractions
{
    /// <summary>
    /// Repository abstraction for managing <see cref="Column"/> persistence.
    /// </summary>
    public interface IColumnRepository
    {
        /// <summary>
        /// Retrieves all columns belonging to a lane.
        /// </summary>
        Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a column by identifier without tracking.
        /// </summary>
        Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a column by identifier with EF Core tracking enabled.
        /// </summary>
        Task<Column?> GetTrackedByIdAsync(Guid columnId, CancellationToken ct = default);

        /// <summary>
        /// Adds a new column to the persistence context.
        /// </summary>
        Task AddAsync(Column column, CancellationToken ct = default);

        /// <summary>
        /// Attempts to rename a column while verifying concurrency and uniqueness constraints.
        /// </summary>
        /// <returns>A <see cref="PrecheckStatus"/> representing the domain precondition result.</returns>
        Task<PrecheckStatus> RenameAsync(
            Guid columnId,
            ColumnName newName,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Begins a two-phase reorder operation for a column.
        /// Phase 1 marks the target column with the new position.
        /// </summary>
        Task<PrecheckStatus> ReorderPhase1Async(
            Guid columnId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Finalizes the reorder operation after phase 1 adjustments are validated.
        /// </summary>
        Task ApplyReorderPhase2Async(Guid columnId, CancellationToken ct = default);

        /// <summary>
        /// Attempts to delete a column after verifying its concurrency token.
        /// </summary>
        Task<PrecheckStatus> DeleteAsync(
            Guid columnId,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Checks whether a column with the same name already exists within the specified lane.
        /// </summary>
        /// <param name="excludeColumnId">Optional column to exclude from the check (used for rename).</param>
        Task<bool> ExistsWithNameAsync
            (Guid laneId,
            ColumnName name,
            Guid? excludeColumnId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Gets the maximum order value among the columns of a given lane.
        /// </summary>
        Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default);
    }
}
