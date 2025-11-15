using Domain.Entities;
using Domain.Enums;

namespace Application.Lanes.Abstractions
{
    /// <summary>
    /// Defines persistence operations for <see cref="Lane"/> entities,
    /// including listing, retrieval, uniqueness checks, ordering queries,
    /// and standard CRUD operations. Retrieval methods provide both
    /// tracked and untracked variants to support read-only scenarios
    /// and domain mutation workflows that rely on EF Core change tracking.
    /// </summary>
    public interface ILaneRepository
    {
        /// <summary>
        /// Retrieves all lanes belonging to the specified project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project whose lanes will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="Lane"/> entities associated with the given project.
        /// </returns>
        Task<IReadOnlyList<Lane>> ListByProjectIdAsync(Guid projectId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a lane by its unique identifier without enabling EF Core tracking.
        /// Suitable for read-only operations or scenarios where update tracking is unnecessary.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="Column"/> entity, or <c>null</c> if no matching lane is found.
        /// </returns>
        Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a lane by its unique identifier with EF Core tracking enabled.
        /// Use this method before mutating the entity so EF Core can detect changes
        /// and persist minimal UPDATE statements automatically.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The tracked <see cref="Lane"/> entity, or <c>null</c> if no matching lane is found.
        /// </returns>
        Task<Lane?> GetByIdForUpdateAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>
        /// Phase 1 of lane reordering within a project. Rebuilds the ordering
        /// in memory using a temporary offset range to avoid unique constraint
        /// violations. Marks affected entities as modified but does not save.
        /// </summary>
        /// <param name="laneId">The identifier of the lane being moved.</param>
        /// <param name="newOrder">The target zero-based order within the project.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// <see cref="PrecheckStatus.NotFound"/> if the lane or project does not exist,
        /// <see cref="PrecheckStatus.NoOp"/> if no reordering is needed,
        /// or <see cref="PrecheckStatus.Ready"/> when changes are prepared.
        /// </returns>
        Task<PrecheckStatus> PrepareReorderAsync(
            Guid laneId,
            int newOrder,
            CancellationToken ct = default);

        /// <summary>
        /// Phase 2 of lane reordering within a project. Assumes temporary offset
        /// orders have already been persisted and normalizes the sequence back
        /// to [0..n]. Marks affected entities as modified but does not save.
        /// </summary>
        /// <param name="laneId">Any lane identifier within the target project.</param>
        /// <param name="ct">Cancellation token.</param>
        Task FinalizeReorderAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>
        /// Determines whether a lane with the given name already exists within a project.
        /// This is typically used to enforce name uniqueness during creation or renaming.
        /// An optional <paramref name="excludeLaneId"/> may be provided to ignore
        /// a specific lane during the check (e.g., when renaming the same lane).
        /// </summary>
        /// <param name="projectId">The project under which the lane name will be validated.</param>
        /// <param name="laneName">The lane name to verify.</param>
        /// <param name="excludeLaneId">
        /// An optional identifier for a lane to exclude from the existence check.
        /// </param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// <c>true</c> if another lane within the project uses the given name; otherwise <c>false</c>.
        /// </returns>
        Task<bool> ExistsWithNameAsync(
            Guid projectId,
            string laneName,
            Guid? excludeLaneId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves the highest <c>Order</c> value among lanes belonging to the specified project.
        /// Useful for appending new lanes at the end of the ordering sequence.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project whose maximum lane order is requested.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The maximum order value found among the project’s lanes,
        /// or <c>0</c> if the project contains no lanes.
        /// </returns>
        Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken ct = default);

        /// <summary>
        /// Adds a new lane to the persistence context.
        /// </summary>
        /// <param name="lane">The lane entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(Lane lane, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing <see cref="Lane"/> entity within the persistence context.
        /// EF Core change tracking will detect modified values and generate minimal UPDATE statements.
        /// </summary>
        /// <param name="lane">The lane entity with modified state.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task UpdateAsync(Lane lane, CancellationToken ct = default);

        /// <summary>
        /// Removes a lane from the persistence context.
        /// </summary>
        /// <param name="lane">The lane entity to remove.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task RemoveAsync(Lane lane, CancellationToken ct = default);
    }
}
