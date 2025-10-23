using Application.Columns.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Services
{
    public sealed class ColumnWriteService(IColumnRepository repo) : IColumnWriteService
    {
        public async Task<(DomainMutation, Column?)> CreateAsync(
            Guid projectId,
            Guid laneId,
            ColumnName name,
            int? order = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name)) return (DomainMutation.NoOp, null);
            if (await repo.ExistsWithNameAsync(laneId, name, excludeColumnId: null, ct)) return (DomainMutation.Conflict, null);

            var isValidOrder = order.HasValue && order.Value >= 0;
            var finalOrder = isValidOrder ?
                Math.Min(order!.Value, await repo.GetMaxOrderAsync(laneId, ct) + 1)
                : await repo.GetMaxOrderAsync(laneId, ct) + 1;

            var column = Column.Create(projectId, laneId, name, finalOrder);
            await repo.AddAsync(column, ct);
            await repo.SaveChangesAsync(ct);

            if (isValidOrder && order!.Value < finalOrder)
            {
                var reorderResult = await repo.ReorderAsync(column.Id, order.Value, column.RowVersion, ct);
                if (reorderResult == DomainMutation.Updated) await repo.SaveChangesAsync(ct);
            }

            return (DomainMutation.Created, column);
        }

        public async Task<DomainMutation> RenameAsync(
            Guid columnId,
            ColumnName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(newName)) return DomainMutation.NoOp;
            return await repo.RenameAsync(columnId, newName, rowVersion, ct);
        }

        public async Task<DomainMutation> ReorderAsync(
            Guid columnId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            if (newOrder < 0) return DomainMutation.NoOp;
            return await repo.ReorderAsync(columnId, newOrder, rowVersion, ct);
        }

        public async Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
            => await repo.DeleteAsync(columnId, rowVersion, ct);
    }
}
