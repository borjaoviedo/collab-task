using Application.Columns.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace TestHelpers.Api.Fakes
{
    public sealed class FakeColumnRepository : IColumnRepository
    {
        private readonly Dictionary<Guid, Column> _columns = [];
        private readonly object _lock = new();

        private static byte[] NextRowVersion() => Guid.NewGuid().ToByteArray();

        public Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Column>>(_columns.Values.Where(c => c.LaneId == laneId)
                .OrderBy(c => c.Order).Select(Clone).ToList());

        public Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default)
            => Task.FromResult(_columns.TryGetValue(columnId, out var c) ? Clone(c) : null);

        public Task<Column?> GetTrackedByIdAsync(Guid columnId, CancellationToken ct = default)
            => Task.FromResult(_columns.TryGetValue(columnId, out var c) ? c : null);

        public Task AddAsync(Column column, CancellationToken ct = default)
        {
            lock (_lock)
            {
                column.SetRowVersion(NextRowVersion());
                _columns[column.Id] = column;
            }
            return Task.CompletedTask;
        }

        public async Task<PrecheckStatus> RenameAsync(
            Guid columnId,
            ColumnName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return PrecheckStatus.NotFound;

            if (!column.RowVersion.SequenceEqual(rowVersion)) return PrecheckStatus.Conflict;
            if (string.Equals(column.Name, newName, StringComparison.Ordinal)) return PrecheckStatus.NoOp;

            if (await ExistsWithNameAsync(column.LaneId, newName, column.Id, ct)) return PrecheckStatus.Conflict;

            column.Rename(newName);
            column.SetRowVersion(NextRowVersion());
            return PrecheckStatus.Ready;
        }

        public async Task<PrecheckStatus> ReorderPhase1Async(
            Guid columnId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            // no real async work here, keep signature for compatibility
            await Task.Yield();

            lock (_lock)
            {
                if (!_columns.TryGetValue(columnId, out var column))
                    return PrecheckStatus.NotFound;

                // Concurrency check
                if (!column.RowVersion.SequenceEqual(rowVersion))
                    return PrecheckStatus.Conflict;

                // Snapshot lane columns ordered by current Order
                var columns = _columns.Values
                    .Where(c => c.LaneId == column.LaneId)
                    .OrderBy(c => c.Order)
                    .ToList();

                var currentIndex = columns.FindIndex(c => c.Id == columnId);
                if (currentIndex < 0) return PrecheckStatus.NotFound;

                var targetIndex = Math.Clamp(newOrder, 0, columns.Count - 1);
                if (currentIndex == targetIndex) return PrecheckStatus.NoOp;

                // Rebuild desired order: remove then insert
                var moving = columns[currentIndex];
                columns.RemoveAt(currentIndex);
                columns.Insert(targetIndex, moving);

                const int OFFSET = 1000;

                // Phase 1: assign temporary unique orders (+OFFSET) and bump RowVersion
                for (int i = 0; i < columns.Count; i++)
                {
                    var tmp = i + OFFSET;
                    if (columns[i].Order != tmp)
                    {
                        columns[i].Reorder(tmp);
                        columns[i].SetRowVersion(NextRowVersion());
                    }
                }

                return PrecheckStatus.Ready;
            }
        }

        public async Task ApplyReorderPhase2Async(Guid columnId, CancellationToken ct = default)
        {
            await Task.Yield();

            lock (_lock)
            {
                if (!_columns.TryGetValue(columnId, out var column))
                    return;

                // Re-read lane with the offsetted orders and normalize to 0..n
                var columns = _columns.Values
                    .Where(c => c.LaneId == column.LaneId)
                    .OrderBy(c => c.Order)
                    .ToList();

                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].Order != i)
                    {
                        columns[i].Reorder(i);
                        columns[i].SetRowVersion(NextRowVersion());
                    }
                }
            }
        }

        public async Task<PrecheckStatus> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default)
        {
            var column = await GetTrackedByIdAsync(columnId, ct);
            if (column is null) return PrecheckStatus.NotFound;
            if (!column.RowVersion.SequenceEqual(rowVersion)) return PrecheckStatus.Conflict;

            _columns.Remove(columnId);
            return PrecheckStatus.Ready;
        }

        public Task<bool> ExistsWithNameAsync(Guid laneId, ColumnName name, Guid? excludeColumnId = null, CancellationToken ct = default)
        {
            var q = _columns.Values.Where(c => c.LaneId == laneId && c.Name == name);

            if (excludeColumnId is Guid id)
                q = q.Where(c => c.Id != id);

            return Task.FromResult(q.Any());
        }

        public Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default)
        {
            var max = _columns.Values
                                .Where(c => c.LaneId == laneId)
                                .Select(c => (int?)c.Order)
                                .DefaultIfEmpty(null)
                                .Max();
            return Task.FromResult(max ?? -1);
        }

        private static Column Clone(Column c)
        {
            var clone = Column.Create(c.ProjectId, c.LaneId, ColumnName.Create(c.Name), c.Order);
            var rowVersion = c.RowVersion is null ? Array.Empty<byte>() : c.RowVersion.ToArray();
            clone.SetRowVersion(rowVersion);
            return clone;
        }
    }
}
