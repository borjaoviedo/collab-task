using Application.Columns.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Services
{
    public sealed class ColumnWriteService(IColumnRepository repo)
    {
        public async Task<(DomainMutation, Column?)> CreateAsync(Guid projectId, Guid laneId, string name, int? order = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name)) return (DomainMutation.NoOp, null);

            if (await repo.ExistsWithNameAsync(laneId, name, null, ct))
                return (DomainMutation.Conflict, null);

            int finalOrder;
            if (order.HasValue && order.Value >= 0)
            {
                finalOrder = Math.Min(order.Value, await repo.GetMaxOrderAsync(laneId, ct) + 1);
            }
            else
            {
                finalOrder = await repo.GetMaxOrderAsync(laneId, ct) + 1;
            }

            var column = Column.Create(projectId, laneId, ColumnName.Create(name), finalOrder);
            await repo.AddAsync(column, ct);
            await repo.SaveChangesAsync(ct);

            if (order.HasValue && order.Value >= 0 && order.Value < finalOrder)
            {
                var res = await repo.ReorderAsync(column.Id, order.Value, column.RowVersion, ct);
                if (res == DomainMutation.Updated) await repo.SaveChangesAsync(ct);
            }

            return (DomainMutation.Created, column);
        }

        public async Task<DomainMutation> RenameAsync(Guid columnId, string newName, byte[] rowVersion, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(newName)) return DomainMutation.NoOp;
            return await repo.RenameAsync(columnId, newName, rowVersion, ct);
        }

        public async Task<DomainMutation> ReorderAsync(Guid columnId, int newOrder, byte[] rowVersion, CancellationToken ct = default)
        {
            if (newOrder < 0) return DomainMutation.NoOp;
            var result = await repo.ReorderAsync(columnId, newOrder, rowVersion, ct);
            return result;
        }

        public async Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
            => await repo.DeleteAsync(columnId, rowVersion, ct);
    }
}
