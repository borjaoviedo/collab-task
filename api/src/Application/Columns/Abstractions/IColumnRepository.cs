using Domain.Entities;
using Domain.Enums;

namespace Application.Columns.Abstractions
{
    /// <summary>
    /// Defines persistence operations for <see cref="Column"/> entities,
    /// including listing, retrieval, uniqueness checks, ordering queries,
    /// and standard CRUD operations. Retrieval methods provide both
    /// tracked and untracked variants to support read-only scenarios
    /// and domain mutation workflows that rely on EF Core change tracking.
    /// </summary>
    public interface IColumnRepository
    {
        /// <summary>
        /// Retrieves all columns belonging to the specified lane.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane whose columns will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="Column"/> entities associated with the given lane.
        /// </returns>
        Task<IReadOnlyList<Column>> ListByLaneIdAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a column by its unique identifier without enabling EF Core tracking.
        /// Suitable for read-only operations or scenarios where update tracking is unnecessary.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="Column"/> entity, or <c>null</c> if no matching column is found.
        /// </returns>
        Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a column by its unique identifier with EF Core tracking enabled.
        /// Use this method before mutating the entity so EF Core can detect changes
        /// and persist minimal UPDATE statements automatically.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The tracked <see cref="Column"/> entity, or <c>null</c> if no matching column is found.
        /// </returns>
        Task<Column?> GetByIdForUpdateAsync(Guid columnId, CancellationToken ct = default);

        /// <summary>
        /// Phase 1 of column reordering within a lane. Rebuilds the ordering
        /// in memory using a temporary offset range to avoid unique constraint
        /// violations. Marks affected entities as modified but does not save.
        /// </summary>
        /// <param name="columnId">The identifier of the column being moved.</param>
        /// <param name="newOrder">The target zero-based order within the lane.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// <see cref="PrecheckStatus.NotFound"/> if the column or lane does not exist,
        /// <see cref="PrecheckStatus.NoOp"/> if no reordering is needed,
        /// or <see cref="PrecheckStatus.Ready"/> when changes are prepared.
        /// </returns>
        Task<PrecheckStatus> PrepareReorderAsync(
            Guid columnId,
            int newOrder,
            CancellationToken ct = default);

        /// <summary>
        /// Phase 2 of column reordering within a lane. Assumes temporary offset
        /// orders have already been persisted and normalizes the sequence back
        /// to [0..n]. Marks affected entities as modified but does not save.
        /// </summary>
        /// <param name="columnId">Any column identifier within the target lane.</param>
        /// <param name="ct">Cancellation token.</param>
        Task FinalizeReorderAsync(Guid columnId, CancellationToken ct = default);

        /// <summary>
        /// Determines whether a column with the given name already exists within a lane.
        /// This is typically used to enforce name uniqueness during creation or renaming.
        /// An optional <paramref name="excludeColumnId"/> may be provided to ignore
        /// a specific column during the check (e.g., when renaming the same column).
        /// </summary>
        /// <param name="laneId">The lane under which the column name will be validated.</param>
        /// <param name="columnName">The column name to verify.</param>
        /// <param name="excludeColumnId">
        /// An optional identifier for a column to exclude from the existence check.
        /// </param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// <c>true</c> if another column within the lane uses the given name; otherwise <c>false</c>.
        /// </returns>
        Task<bool> ExistsWithNameAsync(
            Guid laneId,
            string columnName,
            Guid? excludeColumnId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves the highest <c>Order</c> value among columns belonging to the specified lane.
        /// Useful for appending new columns at the end of the ordering sequence.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane whose maximum column order is requested.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The maximum order value found among the lane’s columns,
        /// or <c>0</c> if the lane contains no columns.
        /// </returns>
        Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>
        /// Adds a new column to the persistence context.
        /// </summary>
        /// <param name="column">The column entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(Column column, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing <see cref="Column"/> entity within the persistence context.
        /// EF Core change tracking will detect modified values and generate minimal UPDATE statements.
        /// </summary>
        /// <param name="column">The column entity with modified state.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task UpdateAsync(Column column, CancellationToken ct = default);

        /// <summary>
        /// Removes a column from the persistence context.
        /// </summary>
        /// <param name="column">The column entity to remove.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task RemoveAsync(Column column, CancellationToken ct = default);
    }
}
