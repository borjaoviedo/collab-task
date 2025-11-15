using Application.Lanes.Abstractions;
using Domain.Entities;
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
