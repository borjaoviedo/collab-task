using Application.Columns.Abstractions;
using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Services
{
    /// <summary>
    /// Write-side application service for columns.
    /// </summary>
    public sealed class ColumnWriteService(IColumnRepository repo, IUnitOfWork uow) : IColumnWriteService
    {
        /// <summary>
        /// Creates a new column and inserts it at the requested position if provided.
        /// </summary>
        /// <param name="projectId">Project owning the column.</param>
        /// <param name="laneId">Lane owning the column.</param>
        /// <param name="name">Column name.</param>
        /// <param name="order">Optional zero-based target position; clamped to [0..max+1].</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The mutation result and the created column when successful.</returns>
        public async Task<(DomainMutation, Column?)> CreateAsync(
            Guid projectId,
            Guid laneId,
            ColumnName name,
            int? order = null,
            CancellationToken ct = default)
        {
            if (await repo.ExistsWithNameAsync(laneId, name, excludeColumnId: null, ct))
                return (DomainMutation.Conflict, null);

            var maxOrder = await repo.GetMaxOrderAsync(laneId, ct);
            var isValidOrder = order is >= 0;
            var finalOrder = isValidOrder ? Math.Min(order!.Value, maxOrder + 1) : maxOrder + 1;

            var column = Column.Create(projectId, laneId, name, finalOrder);
            await repo.AddAsync(column, ct);

            var createResult = await uow.SaveAsync(MutationKind.Create, ct);
            if (createResult != DomainMutation.Created) return (createResult, null);

            // If requested order is before the appended position, run a reorder command
            if (isValidOrder && order!.Value < finalOrder)
            {
                var reorderResult = await ReorderAsync(column.Id, order.Value, column.RowVersion, ct);
                if (reorderResult != DomainMutation.Updated) return (reorderResult, column);
            }

            return (DomainMutation.Created, column);
        }

        /// <summary>
        /// Renames an existing column with concurrency enforcement.
        /// </summary>
        /// <param name="columnId">The column identifier.</param>
        /// <param name="newName">New column name.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A mutation describing the outcome.</returns>
        public async Task<DomainMutation> RenameAsync(
            Guid columnId,
            ColumnName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var status = await repo.RenameAsync(columnId, newName, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            var renameResult = await uow.SaveAsync(MutationKind.Update, ct);
            return renameResult;
        }

        /// <summary>
        /// Reorders a column using a two-phase algorithm to avoid collisions.
        /// </summary>
        /// <param name="columnId">The column identifier.</param>
        /// <param name="newOrder">Target zero-based order.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see cref="DomainMutation.Updated"/> on success or a conflict result.</returns>
        public async Task<DomainMutation> ReorderAsync(
            Guid columnId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var status = await repo.ReorderPhase1Async(columnId, newOrder, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            // First save persists intermediate offsets
            var firstSaveResult = await uow.SaveAsync(MutationKind.Update, ct);
            if (firstSaveResult == DomainMutation.Conflict) return firstSaveResult;

            // Second phase finalizes ordering, then save again
            await repo.ApplyReorderPhase2Async(columnId, ct);
            var secondSaveResult = await uow.SaveAsync(MutationKind.Update, ct);

            return secondSaveResult; // Updated or Conflict
        }

        /// <summary>
        /// Deletes a column with concurrency enforcement.
        /// </summary>
        /// <param name="columnId">The column identifier.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A mutation describing the outcome.</returns>
        public async Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
        {
            var status = await repo.DeleteAsync(columnId, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            var deleteResult = await uow.SaveAsync(MutationKind.Delete, ct);
            return deleteResult;
        }
    }
}
