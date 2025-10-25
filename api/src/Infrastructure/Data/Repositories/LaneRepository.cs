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

        public async Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Id == laneId, ct);

        public async Task<Lane?> GetTrackedByIdAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Lanes.FirstOrDefaultAsync(l => l.Id == laneId, ct);

        public async Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .Where(l => l.ProjectId == projectId)
                        .OrderBy(l => l.Order)
                        .ToListAsync(ct)
                        .ContinueWith(t => (IReadOnlyList<Lane>)t.Result, ct);

        public async Task<bool> ExistsWithNameAsync(Guid projectId, LaneName name, Guid? excludeLaneId = null, CancellationToken ct = default)
        {
            var q = _db.Lanes
                        .AsNoTracking()
                        .Where(l => l.ProjectId == projectId && l.Name == name);

            if (excludeLaneId.HasValue)
                q = q.Where(l => l.Id != excludeLaneId.Value);

            return await q.AnyAsync(ct);
        }

        public async Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken ct = default)
            => await _db.Lanes
                        .AsNoTracking()
                        .Where(l => l.ProjectId == projectId)
                        .Select(l => (int?)l.Order)
                        .MaxAsync(ct) ?? -1;

        public async Task AddAsync(Lane lane, CancellationToken ct = default)
            => await _db.Lanes.AddAsync(lane, ct);

        public async Task<DomainMutation> RenameAsync(Guid laneId, LaneName newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return DomainMutation.NotFound;

            _db.Entry(lane).Property(l => l.RowVersion).OriginalValue = rowVersion;

            // No-op check based on previous name
            var before = lane.Name;
            if (string.Equals(before, newName, StringComparison.Ordinal))
                return DomainMutation.NoOp;

            // Enforce uniqueness (ProjectId, Name)
            if (await ExistsWithNameAsync(lane.ProjectId, newName, lane.Id, ct))
                return DomainMutation.Conflict;

            lane.Rename(LaneName.Create(newName));
            _db.Entry(lane).Property(l => l.Name).IsModified = true;

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Updated;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<DomainMutation> ReorderAsync(Guid laneId, int newOrder, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return DomainMutation.NotFound;

            _db.Entry(lane).Property(l => l.RowVersion).OriginalValue = rowVersion;

            // Load all lanes of the same project ordered by current Order
            var lanes = await _db.Lanes
                .Where(l => l.ProjectId == lane.ProjectId)
                .OrderBy(l => l.Order)
                .ToListAsync(ct);

            var currentIndex = lanes.FindIndex(l => l.Id == laneId);
            if (currentIndex < 0) return DomainMutation.NotFound;

            var targetIndex = Math.Clamp(newOrder, 0, lanes.Count - 1);
            if (currentIndex == targetIndex) return DomainMutation.NoOp;

            // Rebuild target order in-memory: remove then insert
            var moving = lanes[currentIndex];
            lanes.RemoveAt(currentIndex);
            lanes.Insert(targetIndex, moving);

            const int OFFSET = 1000;

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Break (ProjectId, Order) unique index cycles
                // We temporarily move every lane's Order far away to avoid collisions
                // so that EF can produce a valid topological update plan
                foreach (var x in lanes)
                {
                    x.Reorder(x.Order + OFFSET);
                    _db.Entry(x).Property(l => l.Order).IsModified = true;
                }
                await _db.SaveChangesAsync(ct);

                // Apply the final canonical sequence 0..n
                // This guarantees no gaps or duplicates after the move
                for (int i = 0; i < lanes.Count; i++)
                {
                    if (lanes[i].Order != i)
                    {
                        lanes[i].Reorder(i);
                        _db.Entry(lanes[i]).Property(l => l.Order).IsModified = true;
                    }
                }
                await _db.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
                return DomainMutation.Updated;
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency conflict when RowVersion doesn't match or rows changed mid-transaction
                await tx.RollbackAsync(ct);
                return DomainMutation.Conflict;
            }
        }

        public async Task<DomainMutation> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return DomainMutation.NotFound;

            _db.Entry(lane).Property(l => l.RowVersion).OriginalValue = rowVersion;
            _db.Lanes.Remove(lane);

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Deleted;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
            catch (DbUpdateException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);
    }
}
