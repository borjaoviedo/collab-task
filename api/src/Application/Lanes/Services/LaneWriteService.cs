using Application.Lanes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Lanes.Services
{
    public sealed class LaneWriteService(ILaneRepository repo) : ILaneWriteService
    {
        public async Task<(DomainMutation, Lane?)> CreateAsync(
            Guid projectId,
            LaneName name,
            int? order = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name)) return (DomainMutation.NoOp, null);
            if (await repo.ExistsWithNameAsync(projectId, name, excludeLaneId: null, ct)) return (DomainMutation.Conflict, null);

            var isValidOrder = order.HasValue && order.Value >= 0;
            var finalOrder = isValidOrder ?
                Math.Min(order!.Value, await repo.GetMaxOrderAsync(projectId, ct) + 1)
                : await repo.GetMaxOrderAsync(projectId, ct) + 1;

            var lane = Lane.Create(projectId, name, finalOrder);
            await repo.AddAsync(lane, ct);
            await repo.SaveChangesAsync(ct);

            if (isValidOrder && order!.Value < finalOrder)
            {
                var reorderResult = await repo.ReorderAsync(lane.Id, order.Value, lane.RowVersion, ct);
                if (reorderResult == DomainMutation.Updated) await repo.SaveChangesAsync(ct);
            }

            return (DomainMutation.Created, lane);
        }

        public async Task<DomainMutation> RenameAsync(
            Guid laneId,
            LaneName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(newName)) return DomainMutation.NoOp;
            return await repo.RenameAsync(laneId, newName, rowVersion, ct);
        }

        public async Task<DomainMutation> ReorderAsync(
            Guid laneId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            if (newOrder < 0) return DomainMutation.NoOp;
            return await repo.ReorderAsync(laneId, newOrder, rowVersion, ct);
        }

        public async Task<DomainMutation> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default)
            => await repo.DeleteAsync(laneId, rowVersion, ct);
    }
}
