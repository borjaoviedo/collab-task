using Application.Columns.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="Column"/> aggregates.
    /// Provides optimized read and write operations for column entities,
    /// including ordered listing by lane, tracked and untracked retrieval,
    /// name uniqueness checks, ordering utilities, and CRUD persistence.
    /// Read operations rely on <c>AsNoTracking()</c> for performance,
    /// while update operations leverage EF Core change tracking to produce
    /// minimal and efficient UPDATE statements.
    /// </summary>
    /// <param name="db">
    /// The underlying <see cref="CollabTaskDbContext"/> used to query and persist
    /// <see cref="Column"/> entities.
    /// </param>
    public sealed class ColumnRepository(CollabTaskDbContext db) : IColumnRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Column>> ListByLaneIdAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Columns
                        .AsNoTracking()
                        .Where(c => c.LaneId == laneId)
                        .OrderBy(c => c.Order)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default)
            => await _db.Columns
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == columnId, ct);

        /// <inheritdoc/>
        public async Task<Column?> GetByIdForUpdateAsync(Guid columnId, CancellationToken ct = default)
            => await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId, ct);

        /// <inheritdoc/>
        public async Task<PrecheckStatus> PrepareReorderAsync(
            Guid columnId,
            int newOrder,
            CancellationToken ct = default)
        {
            var column = await GetByIdForUpdateAsync(columnId, ct);
            if (column is null) return PrecheckStatus.NotFound;

            var columns = await _db.Columns
                .Where(c => c.LaneId == column.LaneId)
                .OrderBy(c => c.Order)
                .ToListAsync(ct);

            var currentIndex = columns.FindIndex(c => c.Id == columnId);
            if (currentIndex < 0) return PrecheckStatus.NotFound;

            var targetIndex = Math.Clamp(newOrder, 0, columns.Count - 1);
            if (currentIndex == targetIndex) return PrecheckStatus.NoOp;

            const int OFFSET = 1000;

            var moving = columns[currentIndex];
            columns.RemoveAt(currentIndex);
            columns.Insert(targetIndex, moving);

            for (var i = 0; i < columns.Count; i++)
            {
                var tmp = i + OFFSET;
                if (columns[i].Order != tmp)
                {
                    columns[i].Reorder(tmp);
                    _db.Entry(columns[i]).Property(c => c.Order).IsModified = true;
                }
            }

            return PrecheckStatus.Ready;
        }

        /// <inheritdoc/>
        public async Task FinalizeReorderAsync(Guid columnId, CancellationToken ct = default)
        {
            var column = await GetByIdForUpdateAsync(columnId, ct);
            if (column is null) return;

            var columns = await _db.Columns
                .Where(c => c.LaneId == column.LaneId)
                .OrderBy(c => c.Order)
                .ToListAsync(ct);

            for (var i = 0; i < columns.Count; i++)
            {
                if (columns[i].Order != i)
                {
                    columns[i].Reorder(i);
                    _db.Entry(columns[i]).Property(c => c.Order).IsModified = true;
                }
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsWithNameAsync(
            Guid laneId,
            string columnName,
            Guid? excludeColumnId = null,
            CancellationToken ct = default)
        {
            var q = _db.Columns
                        .AsNoTracking()
                        .Where(c => c.LaneId == laneId && c.Name == columnName);

            if (excludeColumnId.HasValue)
                q = q.Where(c => c.Id != excludeColumnId.Value);

            return await q.AnyAsync(ct);
        }

        /// <inheritdoc/>
        public async Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default)
        {
            var max = await _db.Columns
                               .AsNoTracking()
                               .Where(c => c.LaneId == laneId)
                               .Select(c => (int?)c.Order)
                               .MaxAsync(ct);

            return max ?? -1;
        }

        /// <inheritdoc/>
        public async Task AddAsync(Column column, CancellationToken ct = default)
            => await _db.AddAsync(column, ct);

        /// <inheritdoc/>
        public async Task UpdateAsync(Column column, CancellationToken ct = default)
        {
            // If entity is already tracked, do nothing so EF change tracking produces minimal UPDATEs
            if (_db.Entry(column).State == EntityState.Detached)
                _db.Columns.Update(column);

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(Column column, CancellationToken ct = default)
        {
            // Mark entity as deleted; actual deletion occurs in UnitOfWork.SaveAsync()
            _db.Columns.Remove(column);
            await Task.CompletedTask;
        }
    }
}
