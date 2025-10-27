using Application.Lanes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class LaneRepository(AppDbContext db) : ILaneRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .Where(l => l.ProjectId == projectId)
                        .OrderBy(l => l.Order)
                        .ToListAsync(ct)
                        .ContinueWith(t => (IReadOnlyList<Lane>)t.Result, ct);

        public async Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Id == laneId, ct);

        public async Task<Lane?> GetTrackedByIdAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Lanes.FirstOrDefaultAsync(l => l.Id == laneId, ct);

        public async Task AddAsync(Lane lane, CancellationToken ct = default)
            => await _db.Lanes.AddAsync(lane, ct);

        public async Task<PrecheckStatus> RenameAsync(
            Guid laneId,
            LaneName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return PrecheckStatus.NotFound;

            _db.Entry(lane).Property(l => l.RowVersion).OriginalValue = rowVersion;

            // No-op check based on previous name
            var before = lane.Name;
            if (string.Equals(before, newName, StringComparison.Ordinal))
                return PrecheckStatus.NoOp;

            // Enforce uniqueness (ProjectId, Name)
            if (await ExistsWithNameAsync(lane.ProjectId, newName, lane.Id, ct))
                return PrecheckStatus.Conflict;

            lane.Rename(LaneName.Create(newName));
            _db.Entry(lane).Property(l => l.Name).IsModified = true;

            return PrecheckStatus.Ready;
        }

        // Phase 1: apply temporary OFFSET to break (LaneId, Order) unique cycles
        public async Task<PrecheckStatus> ReorderPhase1Async(
            Guid laneId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return PrecheckStatus.NotFound;

            _db.Entry(lane).Property(l => l.RowVersion).OriginalValue = rowVersion;

            var lanes = await _db.Lanes
                .Where(l => l.ProjectId == lane.ProjectId)
                .OrderBy(l => l.Order)
                .ToListAsync(ct);

            var currentIndex = lanes.FindIndex(l => l.Id == laneId);
            if (currentIndex < 0) return PrecheckStatus.NotFound;

            var targetIndex = Math.Clamp(newOrder, 0, lanes.Count - 1);
            if (currentIndex == targetIndex) return PrecheckStatus.NoOp;

            // rebuild order in-memory
            var moving = lanes[currentIndex];
            lanes.RemoveAt(currentIndex);
            lanes.Insert(targetIndex, moving);

            const int OFFSET = 1000;

            // apply temporary unique orders
            for (int i = 0; i < lanes.Count; i++)
            {
                var tmp = i + OFFSET;
                if (lanes[i].Order != tmp)
                {
                    lanes[i].Reorder(tmp);
                    _db.Entry(lanes[i]).Property(l => l.Order).IsModified = true;
                }
            }

            return PrecheckStatus.Ready;
        }

        // Phase 2: write final canonical sequence 0..n.
        public async Task ApplyReorderPhase2Async(Guid laneId, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return;

            var lanes = await _db.Lanes
                .Where(l => l.ProjectId == lane.ProjectId)
                .OrderBy(l => l.Order)
                .ToListAsync(ct);

            for (int i = 0; i < lanes.Count; i++)
            {
                if (lanes[i].Order != i)
                {
                    lanes[i].Reorder(i);
                    _db.Entry(lanes[i]).Property(l => l.Order).IsModified = true;
                }
            }
        }

        public async Task<PrecheckStatus> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return PrecheckStatus.NotFound;

            _db.Entry(lane).Property(l => l.RowVersion).OriginalValue = rowVersion;
            _db.Lanes.Remove(lane);

            return PrecheckStatus.Ready;
        }

        public async Task<bool> ExistsWithNameAsync(
            Guid projectId,
            LaneName name,
            Guid? excludeLaneId = null,
            CancellationToken ct = default)
        {
            var q = _db.Lanes
                        .AsNoTracking()
                        .Where(l => l.ProjectId == projectId && l.Name == name);

            if (excludeLaneId.HasValue)
                q = q.Where(l => l.Id != excludeLaneId.Value);

            return await q.AnyAsync(ct);
        }

        public async Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken ct = default)
        {
            var max = await _db.Lanes
                                .AsNoTracking()
                                .Where(l => l.ProjectId == projectId)
                                .Select(l => (int?)l.Order)
                                .MaxAsync(ct);

            return max ?? -1;
        }
    }
}
