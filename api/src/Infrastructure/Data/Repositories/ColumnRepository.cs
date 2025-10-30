using Application.Columns.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="Column"/> aggregates.
    /// Provides read queries, tracked fetch, creation, rename with uniqueness guard,
    /// two-phase reorder to preserve unique (LaneId, Order), and deletion with concurrency.
    /// </summary>
    public sealed class ColumnRepository(AppDbContext db) : IColumnRepository
    {
        private readonly AppDbContext _db = db;

        /// <summary>
        /// Lists columns within the specified lane ordered by <see cref="Column.Order"/>.
        /// </summary>
        /// <param name="laneId">Lane identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Columns
                        .AsNoTracking()
                        .Where(c => c.LaneId == laneId)
                        .OrderBy(c => c.Order)
                        .ToListAsync(ct);

        /// <summary>
        /// Gets a column by id without tracking.
        /// </summary>
        /// <param name="columnId">Column identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default)
            => await _db.Columns
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == columnId, ct);

        /// <summary>
        /// Gets a column by id with change tracking enabled.
        /// </summary>
        /// <param name="columnId">Column identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Column?> GetTrackedByIdAsync(Guid columnId, CancellationToken ct = default)
            => await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId, ct);

        /// <summary>
        /// Adds a new column to the context.
        /// </summary>
        public async Task AddAsync(Column column, CancellationToken ct = default)
            => await _db.AddAsync(column, ct);

        /// <summary>
        /// Renames a column with optimistic concurrency and uniqueness checks.
        /// </summary>
        /// <param name="columnId">Target column id.</param>
        /// <param name="newName">Proposed name.</param>
        /// <param name="rowVersion">Original concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><see cref="PrecheckStatus"/> describing readiness or conflicts.</returns>
        public async Task<PrecheckStatus> RenameAsync(
            Guid columnId,
            ColumnName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return PrecheckStatus.NotFound;

            // Attach original row version for optimistic concurrency
            _db.Entry(column).Property(c => c.RowVersion).OriginalValue = rowVersion;

            // No-op if the name is identical
            if (string.Equals(column.Name, newName, StringComparison.Ordinal))
                return PrecheckStatus.NoOp;

            // Enforce uniqueness within the same lane
            if (await ExistsWithNameAsync(column.LaneId, newName, column.Id, ct))
                return PrecheckStatus.Conflict;

            column.Rename(newName);
            _db.Entry(column).Property(c => c.Name).IsModified = true;

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Phase 1 of reorder: assigns a temporary offset to all items to avoid unique index collisions
        /// on (LaneId, Order) while moving the target to the desired index.
        /// </summary>
        /// <param name="columnId">Target column id.</param>
        /// <param name="newOrder">Desired zero-based order.</param>
        /// <param name="rowVersion">Original concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<PrecheckStatus> ReorderPhase1Async(
            Guid columnId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return PrecheckStatus.NotFound;

            _db.Entry(column).Property(c => c.RowVersion).OriginalValue = rowVersion;

            var columns = await _db.Columns
                .Where(c => c.LaneId == column.LaneId)
                .OrderBy(c => c.Order)
                .ToListAsync(ct);

            var currentIndex = columns.FindIndex(c => c.Id == columnId);
            if (currentIndex < 0) return PrecheckStatus.NotFound;

            var targetIndex = Math.Clamp(newOrder, 0, columns.Count - 1);
            if (currentIndex == targetIndex) return PrecheckStatus.NoOp;

            // Rebuild new order in memory
            var moving = columns[currentIndex];
            columns.RemoveAt(currentIndex);
            columns.Insert(targetIndex, moving);

            const int OFFSET = 1000;

            // Apply temporary unique orders to break unique cycles
            for (int i = 0; i < columns.Count; i++)
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

        /// <summary>
        /// Phase 2 of reorder: writes the final canonical sequence [0..n].
        /// </summary>
        /// <param name="columnId">Any column id in the target lane.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task ApplyReorderPhase2Async(Guid columnId, CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return;

            var columns = await _db.Columns
                .Where(c => c.LaneId == column.LaneId)
                .OrderBy(c => c.Order)
                .ToListAsync(ct);

            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].Order != i)
                {
                    columns[i].Reorder(i);
                    _db.Entry(columns[i]).Property(c => c.Order).IsModified = true;
                }
            }
        }

        /// <summary>
        /// Deletes a column with optimistic concurrency protection.
        /// </summary>
        public async Task<PrecheckStatus> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return PrecheckStatus.NotFound;

            _db.Entry(column).Property(c => c.RowVersion).OriginalValue = rowVersion;
            _db.Columns.Remove(column);

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Checks existence of a column name within a lane, optionally excluding a given column id.
        /// </summary>
        public async Task<bool> ExistsWithNameAsync(
            Guid laneId,
            ColumnName name,
            Guid? excludeColumnId = null,
            CancellationToken ct = default)
        {
            var q = _db.Columns
                        .AsNoTracking()
                        .Where(c => c.LaneId == laneId && c.Name == name);

            if (excludeColumnId.HasValue)
                q = q.Where(c => c.Id != excludeColumnId.Value);

            return await q.AnyAsync(ct);
        }

        /// <summary>
        /// Returns the maximum <see cref="Column.Order"/> for the given lane, or -1 if none exist.
        /// </summary>
        public async Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default)
        {
            var max = await _db.Columns
                               .AsNoTracking()
                               .Where(c => c.LaneId == laneId)
                               .Select(c => (int?)c.Order)
                               .MaxAsync(ct);

            return max ?? -1;
        }
    }

}
