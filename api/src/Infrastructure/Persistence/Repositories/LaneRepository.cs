using Application.Lanes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="Lane"/> aggregates.
    /// Supports listing by project, tracked fetch, creation, rename with uniqueness guard,
    /// two-phase reorder to preserve unique (ProjectId, Order), and deletion with concurrency.
    /// </summary>
    public sealed class LaneRepository(CollabTaskDbContext db) : ILaneRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <summary>
        /// Lists lanes within the specified project ordered by <see cref="Lane.Order"/>.
        /// </summary>
        public async Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .Where(l => l.ProjectId == projectId)
                        .OrderBy(l => l.Order)
                        .ToListAsync(ct)
                        .ContinueWith(t => (IReadOnlyList<Lane>)t.Result, ct);

        /// <summary>Gets a lane by id without tracking.</summary>
        public async Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Id == laneId, ct);

        /// <summary>Gets a lane by id with tracking.</summary>
        public async Task<Lane?> GetTrackedByIdAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Lanes.FirstOrDefaultAsync(l => l.Id == laneId, ct);

        /// <summary>Adds a new lane to the context.</summary>
        public async Task AddAsync(Lane lane, CancellationToken ct = default)
            => await _db.Lanes.AddAsync(lane, ct);

        /// <summary>
        /// Renames a lane with optimistic concurrency and uniqueness checks on (ProjectId, Name).
        /// </summary>
        public async Task<PrecheckStatus> RenameAsync(
            Guid laneId,
            LaneName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return PrecheckStatus.NotFound;

            _db.Entry(lane).Property(l => l.RowVersion).OriginalValue = rowVersion;

            var before = lane.Name;
            if (string.Equals(before, newName, StringComparison.Ordinal))
                return PrecheckStatus.NoOp;

            if (await ExistsWithNameAsync(lane.ProjectId, newName, lane.Id, ct))
                return PrecheckStatus.Conflict;

            // Use VO factory to preserve normalization rules
            lane.Rename(LaneName.Create(newName));
            _db.Entry(lane).Property(l => l.Name).IsModified = true;

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Phase 1 of reorder: assigns a temporary offset to avoid unique index collisions
        /// on (ProjectId, Order) while moving the target to the desired index.
        /// </summary>
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

            var moving = lanes[currentIndex];
            lanes.RemoveAt(currentIndex);
            lanes.Insert(targetIndex, moving);

            const int OFFSET = 1000;

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

        /// <summary>
        /// Phase 2 of reorder: writes the final canonical sequence [0..n].
        /// </summary>
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

        /// <summary>
        /// Soft-delete intent: marks the lane for deletion with concurrency protection.
        /// </summary>
        public async Task<PrecheckStatus> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return PrecheckStatus.NotFound;

            _db.Entry(lane).Property(l => l.RowVersion).OriginalValue = rowVersion;
            _db.Lanes.Remove(lane);

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Checks if a lane name exists within a project, optionally excluding a lane id.
        /// </summary>
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

        /// <summary>
        /// Returns the maximum <see cref="Lane.Order"/> for the given project, or -1 if none exist.
        /// </summary>
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
