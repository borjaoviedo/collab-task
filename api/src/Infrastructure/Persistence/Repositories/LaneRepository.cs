using Application.Lanes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="Lane"/> aggregates.
    /// Provides optimized read and write operations for lane entities,
    /// including ordered listing by project, tracked and untracked retrieval,
    /// name uniqueness checks, ordering utilities, and CRUD persistence.
    /// Read operations rely on <c>AsNoTracking()</c> for performance,
    /// while update operations leverage EF Core change tracking to produce
    /// minimal and efficient UPDATE statements.
    /// </summary>
    /// <param name="db">
    /// The underlying <see cref="CollabTaskDbContext"/> used to query and persist
    /// <see cref="Lane"/> entities.
    /// </param>
    public sealed class LaneRepository(CollabTaskDbContext db) : ILaneRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Lane>> ListByProjectIdAsync(Guid projectId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .Where(l => l.ProjectId == projectId)
                        .OrderBy(l => l.Order)
                        .ToListAsync(ct)
                        .ContinueWith(t => (IReadOnlyList<Lane>)t.Result, ct);

        /// <inheritdoc/>
        public async Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Id == laneId, ct);

        /// <inheritdoc/>
        public async Task<Lane?> GetByIdForUpdateAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Lanes.FirstOrDefaultAsync(l => l.Id == laneId, ct);

        /// <inheritdoc/>
        public async Task<PrecheckStatus> PrepareReorderAsync(
            Guid laneId,
            int newOrder,
            CancellationToken ct = default)
        {
            var lane = await GetByIdForUpdateAsync(laneId, ct);
            if (lane is null) return PrecheckStatus.NotFound;

            var lanes = await _db.Lanes
                .Where(l => l.ProjectId == lane.ProjectId)
                .OrderBy(l => l.Order)
                .ToListAsync(ct);

            var currentIndex = lanes.FindIndex(l => l.Id == laneId);
            if (currentIndex < 0) return PrecheckStatus.NotFound;

            var targetIndex = Math.Clamp(newOrder, 0, lanes.Count - 1);
            if (currentIndex == targetIndex) return PrecheckStatus.NoOp;

            const int OFFSET = 1000;

            var moving = lanes[currentIndex];
            lanes.RemoveAt(currentIndex);
            lanes.Insert(targetIndex, moving);

            for (var i = 0; i < lanes.Count; i++)
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

        /// <inheritdoc/>
        public async Task FinalizeReorderAsync(Guid laneId, CancellationToken ct = default)
        {
            var lane = await GetByIdForUpdateAsync(laneId, ct);
            if (lane is null) return;

            var lanes = await _db.Lanes
                .Where(l => l.ProjectId == lane.ProjectId)
                .OrderBy(l => l.Order)
                .ToListAsync(ct);

            for (var i = 0; i < lanes.Count; i++)
            {
                if (lanes[i].Order != i)
                {
                    lanes[i].Reorder(i);
                    _db.Entry(lanes[i]).Property(l => l.Order).IsModified = true;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsWithNameAsync(
            Guid projectId,
            string laneName,
            Guid? excludeLaneId = null,
            CancellationToken ct = default)
        {
            var q = _db.Lanes
                       .AsNoTracking()
                       .Where(l => l.ProjectId == projectId && l.Name == laneName);

            if (excludeLaneId.HasValue)
                q = q.Where(l => l.Id != excludeLaneId.Value);

            return await q.AnyAsync(ct);
        }

        /// <inheritdoc/>
        public async Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken ct = default)
        {
            var max = await _db.Lanes
                               .AsNoTracking()
                               .Where(l => l.ProjectId == projectId)
                               .Select(l => (int?)l.Order)
                               .MaxAsync(ct);

            return max ?? -1;
        }

        /// <inheritdoc/>
        public async Task AddAsync(Lane lane, CancellationToken ct = default)
            => await _db.Lanes.AddAsync(lane, ct);

        /// <inheritdoc/>
        public async Task UpdateAsync(Lane lane, CancellationToken ct = default)
        {
            // If entity is already tracked, do nothing so EF change tracking produces minimal UPDATEs
            if (_db.Entry(lane).State == EntityState.Detached)
                _db.Lanes.Update(lane);

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(Lane lane, CancellationToken ct = default)
        {
            // Mark entity as deleted; actual deletion occurs in UnitOfWork.SaveAsync()
            _db.Lanes.Remove(lane);
            await Task.CompletedTask;
        }
    }
}
