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

        public async Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default)
            => await _db.Columns
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == columnId, ct);

        public async Task<Column?> GetTrackedByIdAsync(Guid columnId, CancellationToken ct = default)
            => await _db.Columns.FirstOrDefaultAsync(c => c.Id == columnId, ct);

        public async Task<bool> ExistsWithNameAsync(Guid laneId, string name, Guid? excludeColumnId = null, CancellationToken ct = default)
        {
            var q = _db.Columns.AsNoTracking().Where(c => c.LaneId == laneId && c.Name == name);
            if (excludeColumnId.HasValue) q = q.Where(c => c.Id != excludeColumnId.Value);
            return await q.AnyAsync(ct);
        }

        public async Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Columns
                        .Where(c => c.LaneId == laneId)
                        .Select(c => (int?)c.Order)
                        .MaxAsync(ct) ?? -1;

        public async Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default)
            => await _db.Columns
                        .AsNoTracking()
                        .Where(c => c.LaneId == laneId)
                        .OrderBy(c => c.Order)
                        .ToListAsync(ct);

        public async Task AddAsync(Column column, CancellationToken ct = default)
            => await _db.AddAsync(column, ct);

        public async Task<DomainMutation> RenameAsync(Guid columnId, string newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return DomainMutation.NotFound;

            _db.Entry(column).Property(c => c.RowVersion).OriginalValue = rowVersion;

            // No-op check based on previous name
            var before = column.Name;
            if (string.Equals(before, newName, StringComparison.Ordinal))
                return DomainMutation.NoOp;

            // Enforce uniqueness (LaneId, Name)
            if (await ExistsWithNameAsync(column.LaneId, newName, column.Id, ct))
                return DomainMutation.Conflict;

            column.Rename(ColumnName.Create(newName));
            _db.Entry(column).Property(c => c.Name).IsModified = true;

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

        public async Task<DomainMutation> ReorderAsync(Guid columnId, int newOrder, byte[] rowVersion, CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return DomainMutation.NotFound;

            _db.Entry(column).Property(c => c.RowVersion).OriginalValue = rowVersion;

            // Load all columns of the same lane ordered by current Order
            var columns = await _db.Columns
                .Where(c => c.LaneId == column.LaneId)
                .OrderBy(c => c.Order)
                .ToListAsync(ct);

            var currentIndex = columns.FindIndex(c => c.Id == columnId);
            if (currentIndex < 0) return DomainMutation.NotFound;

            var targetIndex = Math.Clamp(newOrder, 0, columns.Count - 1);
            if (currentIndex == targetIndex) return DomainMutation.NoOp;

            // Rebuild target order in-memory: remove then insert
            var moving = columns[currentIndex];
            columns.RemoveAt(currentIndex);
            columns.Insert(targetIndex, moving);

            const int OFFSET = 1000;

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Break (LaneId, Order) unique index cycles
                // We temporarily move every column's Order far away to avoid collisions
                // so that EF can produce a valid topological update plan
                foreach (var x in columns)
                {
                    x.Reorder(x.Order + OFFSET);
                    _db.Entry(x).Property(c => c.Order).IsModified = true;
                }
                await _db.SaveChangesAsync(ct);

                // Apply the final canonical sequence 0..n
                // This guarantees no gaps or duplicates after the move
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].Order != i)
                    {
                        columns[i].Reorder(i);
                        _db.Entry(columns[i]).Property(c => c.Order).IsModified = true;
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

        public async Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return DomainMutation.NotFound;

            _db.Entry(column).Property(c => c.RowVersion).OriginalValue = rowVersion;
            _db.Columns.Remove(column);

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

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
    }
}
