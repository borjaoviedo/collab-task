using Application.Columns.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class ColumnRepository(AppDbContext db) : IColumnRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Columns
                        .AsNoTracking()
                        .Where(c => c.LaneId == laneId)
                        .OrderBy(c => c.Order)
                        .ToListAsync(ct);

        public async Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default)
            => await _db.Columns
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == columnId, ct);

        public async Task<Column?> GetTrackedByIdAsync(Guid columnId, CancellationToken ct = default)
            => await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId, ct);

        public async Task AddAsync(Column column, CancellationToken ct = default)
            => await _db.AddAsync(column, ct);

        public async Task<PrecheckStatus> RenameAsync(
            Guid columnId,
            ColumnName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return PrecheckStatus.NotFound;

            _db.Entry(column).Property(c => c.RowVersion).OriginalValue = rowVersion;

            // No-op check based on previous name
            if (string.Equals(column.Name, newName, StringComparison.Ordinal))
                return PrecheckStatus.NoOp;

            // Enforce uniqueness (LaneId, Name)
            if (await ExistsWithNameAsync(column.LaneId, newName, column.Id, ct))
                return PrecheckStatus.Conflict;

            column.Rename(newName);
            _db.Entry(column).Property(c => c.Name).IsModified = true;

            return PrecheckStatus.Ready;
        }

        // Phase 1: apply temporary OFFSET to break (LaneId, Order) unique cycles
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

            // rebuild order in-memory
            var moving = columns[currentIndex];
            columns.RemoveAt(currentIndex);
            columns.Insert(targetIndex, moving);

            const int OFFSET = 1000;

            // apply temporary unique orders
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

        // Phase 2: write final canonical sequence 0..n.
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

        public async Task<PrecheckStatus> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return PrecheckStatus.NotFound;

            _db.Entry(column).Property(c => c.RowVersion).OriginalValue = rowVersion;
            _db.Columns.Remove(column);

            return PrecheckStatus.Ready;
        }

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
