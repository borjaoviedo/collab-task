using Application.Columns.Abstractions;
using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Services
{
    public sealed class ColumnWriteService(IColumnRepository repo, IUnitOfWork uow) : IColumnWriteService
    {
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

            // Reorder to insert at the requested position
            if (isValidOrder && order!.Value < finalOrder)
            {
                var reorderResult = await ReorderAsync(column.Id, order.Value, column.RowVersion, ct);
                if (reorderResult != DomainMutation.Updated) return (reorderResult, column);
            }

            return (DomainMutation.Created, column);
        }

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

        public async Task<DomainMutation> ReorderAsync(
            Guid columnId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var status = await repo.ReorderPhase1Async(columnId, newOrder, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            // 1st save: persists OFFSET orders
            var firstSaveResult = await uow.SaveAsync(MutationKind.Update, ct);
            if (firstSaveResult == DomainMutation.Conflict) return firstSaveResult;

            // Phase 2: then second save
            await repo.ApplyReorderPhase2Async(columnId, ct);
            var secondSaveResult = await uow.SaveAsync(MutationKind.Update, ct);

            return secondSaveResult; // Updated or Conflict
        }

        public async Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
        {
            var status = await repo.DeleteAsync(columnId, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            var deleteResult = await uow.SaveAsync(MutationKind.Delete, ct);
            return deleteResult;
        }
    }
}
