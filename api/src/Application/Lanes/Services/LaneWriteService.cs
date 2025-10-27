using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Application.Lanes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Lanes.Services
{
    public sealed class LaneWriteService(ILaneRepository repo, IUnitOfWork uow) : ILaneWriteService
    {
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

            // Reorder to insert at the requested position
            if (isValidOrder && order!.Value < finalOrder)
            {
                var reorderResult = await ReorderAsync(lane.Id, order.Value, lane.RowVersion, ct);
                if (reorderResult != DomainMutation.Updated) return (reorderResult, lane);
            }

            return (DomainMutation.Created, lane);
        }

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

        public async Task<DomainMutation> ReorderAsync(
            Guid laneId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var status = await repo.ReorderPhase1Async(laneId, newOrder, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            // 1st save: persists OFFSET orders
            var firstSaveResult = await uow.SaveAsync(MutationKind.Update, ct);
            if (firstSaveResult == DomainMutation.Conflict) return firstSaveResult;

            // Phase 2: then second save
            await repo.ApplyReorderPhase2Async(laneId, ct);
            var secondSaveResult = await uow.SaveAsync(MutationKind.Update, ct);

            return secondSaveResult; // Updated or Conflict
        }

        public async Task<DomainMutation> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default)
        {
            var status = await repo.DeleteAsync(laneId, rowVersion, ct);
            if (status != PrecheckStatus.Ready) return status.ToErrorDomainMutation();

            var deleteResult = await uow.SaveAsync(MutationKind.Delete, ct);
            return deleteResult;
        }
    }
}
