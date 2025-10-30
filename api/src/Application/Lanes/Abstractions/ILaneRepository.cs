using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Lanes.Abstractions
{
    /// <summary>
    /// Defines persistence operations for lane entities.
    /// </summary>
    public interface ILaneRepository
    {
        /// <summary>Lists all lanes of a given project.</summary>
        Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);

        /// <summary>Gets a lane by its identifier without tracking.</summary>
        Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>Gets a lane by its identifier with change tracking enabled.</summary>
        Task<Lane?> GetTrackedByIdAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>Adds a new lane to the persistence context.</summary>
        Task AddAsync(Lane lane, CancellationToken ct = default);

        /// <summary>Renames an existing lane, enforcing concurrency via row version.</summary>
        Task<PrecheckStatus> RenameAsync(
            Guid laneId,
            LaneName newName,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Performs the first step of a reorder operation with prechecks.</summary>
        Task<PrecheckStatus> ReorderPhase1Async(
            Guid laneId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Finalizes the reorder after phase one succeeds.</summary>
        Task ApplyReorderPhase2Async(Guid laneId, CancellationToken ct = default);

        /// <summary>Deletes a lane if concurrency and constraints allow it.</summary>
        Task<PrecheckStatus> DeleteAsync(
            Guid laneId,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Checks whether a lane name already exists within a project.</summary>
        Task<bool> ExistsWithNameAsync(
            Guid projectId,
            LaneName name,
            Guid? excludeLaneId = null,
            CancellationToken ct = default);

        /// <summary>Gets the highest current order value among lanes of a project.</summary>
        Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken ct = default);
    }
}
