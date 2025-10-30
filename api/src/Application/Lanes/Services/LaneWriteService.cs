using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Application.Lanes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Lanes.Services
{
    /// <summary>
    /// Write-side application service for lanes.
    /// </summary>
    public sealed class LaneWriteService(ILaneRepository repo, IUnitOfWork uow) : ILaneWriteService
    {
        /// <summary>
        /// Creates a new lane and inserts it at the requested position if provided.
        /// </summary>
        /// <param name="projectId">Project owning the lane.</param>
        /// <param name="name">Lane name.</param>
        /// <param name="order">Optional zero-based target position; clamped to [0..max+1].</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The mutation result and the created lane when successful.</returns>
        public async Task<(DomainMutation, Lane?)> CreateAsync(
            Guid projectId,
            LaneName name,
            int? order = null,
            CancellationToken ct = default)
        {
            if (await repo.ExistsWithNameAsync(projectId, name, excludeLaneId: null, ct))
                return (DomainMutation.Conflict, null);

            var maxOrder = await repo.GetMaxOrderAsync(projectId, ct);
            var isValidOrder = order is >= 0;
            var finalOrder = isValidOrder ? Math.Min(order!.Value, maxOrder + 1) : maxOrder + 1;

            var lane = Lane.Create(projectId, name, finalOrder);
            await repo.AddAsync(lane, ct);

            var createResult = await uow.SaveAsync(MutationKind.Create, ct);
            if (createResult != DomainMutation.Created) return (createResult, null);

            // If requested order is before the appended position, run a reorder command
            if (isValidOrder && order!.Value < finalOrder)
            {
                var reorderResult = await ReorderAsync(lane.Id, order.Value, lane.RowVersion, ct);
                if (reorderResult != DomainMutation.Updated) return (reorderResult, lane);
            }

            return (DomainMutation.Created, lane);
        }

        /// <summary>
        /// Renames an existing lane with concurrency enforcement.
        /// </summary>
        /// <param name="laneId">The lane identifier.</param>
        /// <param name="newName">New lane name.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A mutation describing the outcome.</returns>
        public async Task<DomainMutation> RenameAsync(
            Guid laneId,
            LaneName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var status = await repo.RenameAsync(laneId, newName, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            var renameResult = await uow.SaveAsync(MutationKind.Update, ct);
            return renameResult;
        }

        /// <summary>
        /// Reorders a lane using a two-phase algorithm to avoid collisions.
        /// </summary>
        /// <param name="laneId">The lane identifier.</param>
        /// <param name="newOrder">Target zero-based order.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see cref="DomainMutation.Updated"/> on success or a conflict result.</returns>
        public async Task<DomainMutation> ReorderAsync(
            Guid laneId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var status = await repo.ReorderPhase1Async(laneId, newOrder, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            // First save persists intermediate offsets.
            var firstSaveResult = await uow.SaveAsync(MutationKind.Update, ct);
            if (firstSaveResult == DomainMutation.Conflict) return firstSaveResult;

            // Second phase finalizes ordering, then save again.
            await repo.ApplyReorderPhase2Async(laneId, ct);
            var secondSaveResult = await uow.SaveAsync(MutationKind.Update, ct);

            return secondSaveResult; // Updated or Conflict
        }

        /// <summary>
        /// Deletes a lane with concurrency enforcement.
        /// </summary>
        /// <param name="laneId">The lane identifier.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A mutation describing the outcome.</returns>
        public async Task<DomainMutation> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default)
        {
            var status = await repo.DeleteAsync(laneId, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            var deleteResult = await uow.SaveAsync(MutationKind.Delete, ct);
            return deleteResult;
        }
    }
}
