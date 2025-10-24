using Application.Columns.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Api.Tests.Fakes
{
    public sealed class FakeColumnRepository : IColumnRepository
    {
        private readonly Dictionary<Guid, Column> _columns = [];
        private readonly object _lock = new();

        private static byte[] NextRowVersion() => Guid.NewGuid().ToByteArray();

        public Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default)
            => Task.FromResult(_columns.TryGetValue(columnId, out var c) ? Clone(c) : null);

        public Task<Column?> GetTrackedByIdAsync(Guid columnId, CancellationToken ct = default)
            => Task.FromResult(_columns.TryGetValue(columnId, out var c) ? c : null);

        public Task<bool> ExistsWithNameAsync(Guid laneId, ColumnName name, Guid? excludeColumnId = null, CancellationToken ct = default)
        {
            var q = _columns.Values.Where(c => c.LaneId == laneId && c.Name == name);
            if (excludeColumnId is Guid id) q = q.Where(c => c.Id != id);
            return Task.FromResult(q.Any());
        }

        public Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default)
        {
            var max = _columns.Values.Where(c => c.LaneId == laneId).Select(c => (int?)c.Order).DefaultIfEmpty(null).Max();
            return Task.FromResult(max ?? -1);
        }

        public Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Column>>(_columns.Values.Where(c => c.LaneId == laneId)
                .OrderBy(c => c.Order).Select(Clone).ToList());

        public Task AddAsync(Column column, CancellationToken ct = default)
        {
            lock (_lock)
            {
                column.RowVersion = NextRowVersion();
                _columns[column.Id] = column;
            }
            return Task.CompletedTask;
        }

        public async Task<DomainMutation> RenameAsync(Guid columnId, ColumnName newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var col = await GetTrackedByIdAsync(columnId, ct);
            if (col is null) return DomainMutation.NotFound;

            if (!col.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;
            if (string.Equals(col.Name, newName, StringComparison.Ordinal)) return DomainMutation.NoOp;

            if (await ExistsWithNameAsync(col.LaneId, newName, col.Id, ct)) return DomainMutation.Conflict;

            col.Rename(newName);
            col.RowVersion = NextRowVersion();
            return DomainMutation.Updated;
        }

        public async Task<DomainMutation> ReorderAsync(Guid columnId, int newOrder, byte[] rowVersion, CancellationToken ct = default)
        {
            var col = await GetTrackedByIdAsync(columnId, ct);
            if (col is null) return DomainMutation.NotFound;
            if (!col.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;

            var cols = _columns.Values.Where(c => c.LaneId == col.LaneId).OrderBy(c => c.Order).ToList();
            var currentIndex = cols.FindIndex(c => c.Id == columnId);
            if (currentIndex < 0) return DomainMutation.NotFound;

            var targetIndex = Math.Clamp(newOrder, 0, cols.Count - 1);
            if (currentIndex == targetIndex) return DomainMutation.NoOp;

            var moving = cols[currentIndex];
            cols.RemoveAt(currentIndex);
            cols.Insert(targetIndex, moving);

            for (int i = 0; i < cols.Count; i++)
            {
                if (cols[i].Order != i)
                {
                    cols[i].Reorder(i);
                    cols[i].RowVersion = NextRowVersion();
                }
            }
            return DomainMutation.Updated;
        }

        public async Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
        {
            var col = await GetTrackedByIdAsync(columnId, ct);
            if (col is null) return DomainMutation.NotFound;
            if (!col.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;

            _columns.Remove(columnId);
            return DomainMutation.Deleted;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        private static Column Clone(Column c)
        {
            var clone = Column.Create(c.ProjectId, c.LaneId, ColumnName.Create(c.Name), c.Order);
            clone.RowVersion = (c.RowVersion is null) ? Array.Empty<byte>() : c.RowVersion.ToArray();
            return clone;
        }
    }
}
