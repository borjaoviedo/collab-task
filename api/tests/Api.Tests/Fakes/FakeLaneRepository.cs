using Application.Lanes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Api.Tests.Fakes
{
    public sealed class FakeLaneRepository : ILaneRepository
    {
        private readonly Dictionary<Guid, Lane> _lanes = [];
        private readonly object _lock = new();

        public Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Lane>>(_lanes.Values.Where(l => l.ProjectId == projectId)
                .OrderBy(l => l.Order).Select(Clone).ToList());

        public Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default)
            => Task.FromResult(_lanes.TryGetValue(laneId, out var l) ? Clone(l) : null);

        public Task<Lane?> GetTrackedByIdAsync(Guid laneId, CancellationToken ct = default)
            => Task.FromResult(_lanes.TryGetValue(laneId, out var l) ? l : null);

        public Task AddAsync(Lane lane, CancellationToken ct = default)
        {
            lock (_lock)
            {
                lane.SetRowVersion(NextRowVersion());
                _lanes[lane.Id] = lane;
            }
            return Task.CompletedTask;
        }

        public async Task<PrecheckStatus> RenameAsync(Guid laneId, LaneName newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return PrecheckStatus.NotFound;

            if (!lane.RowVersion.SequenceEqual(rowVersion)) return PrecheckStatus.Conflict;
            if (string.Equals(lane.Name, newName, StringComparison.Ordinal)) return PrecheckStatus.NoOp;

            if (await ExistsWithNameAsync(lane.ProjectId, newName, lane.Id, ct)) return PrecheckStatus.Conflict;

            lane.Rename(LaneName.Create(newName));
            lane.SetRowVersion(NextRowVersion());
            return PrecheckStatus.Ready;
        }

        public async Task<PrecheckStatus> ReorderPhase1Async(
            Guid laneId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            // no real async work here, keep signature for compatibility
            await Task.Yield();

            lock (_lock)
            {
                if (!_lanes.TryGetValue(laneId, out var lane))
                    return PrecheckStatus.NotFound;

                // Concurrency check
                if (!lane.RowVersion.SequenceEqual(rowVersion))
                    return PrecheckStatus.Conflict;

                // Snapshot lane lane ordered by current Order
                var lanes = _lanes.Values
                    .Where(l => l.ProjectId == lane.ProjectId)
                    .OrderBy(l => l.Order)
                    .ToList();

                var currentIndex = lanes.FindIndex(l => l.Id == laneId);
                if (currentIndex < 0) return PrecheckStatus.NotFound;

                var targetIndex = Math.Clamp(newOrder, 0, lanes.Count - 1);
                if (currentIndex == targetIndex) return PrecheckStatus.NoOp;

                // Rebuild desired order: remove then insert
                var moving = lanes[currentIndex];
                lanes.RemoveAt(currentIndex);
                lanes.Insert(targetIndex, moving);

                const int OFFSET = 1000;

                // Phase 1: assign temporary unique orders (+OFFSET) and bump RowVersion
                for (int i = 0; i < lanes.Count; i++)
                {
                    var tmp = i + OFFSET;
                    if (lanes[i].Order != tmp)
                    {
                        lanes[i].Reorder(tmp);
                        lanes[i].SetRowVersion(NextRowVersion());
                    }
                }

                return PrecheckStatus.Ready;
            }
        }

        public async Task ApplyReorderPhase2Async(Guid laneId, CancellationToken ct = default)
        {
            await Task.Yield();

            lock (_lock)
            {
                if (!_lanes.TryGetValue(laneId, out var lane))
                    return;

                var lanes = _lanes.Values
                    .Where(l => l.ProjectId == lane.ProjectId)
                    .OrderBy(l => l.Order)
                    .ToList();

                for (int i = 0; i < lanes.Count; i++)
                {
                    if (lanes[i].Order != i)
                    {
                        lanes[i].Reorder(i);
                        lanes[i].SetRowVersion(NextRowVersion());
                    }
                }
            }
        }

        public async Task<PrecheckStatus> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return PrecheckStatus.NotFound;
            if (!lane.RowVersion.SequenceEqual(rowVersion)) return PrecheckStatus.Conflict;

            _lanes.Remove(laneId);
            return PrecheckStatus.Ready;
        }

        public Task<bool> ExistsWithNameAsync(Guid projectId, LaneName name, Guid? excludeLaneId = null, CancellationToken ct = default)
        {
            var q = _lanes.Values.Where(l => l.ProjectId == projectId && l.Name == name);
            if (excludeLaneId is Guid id) q = q.Where(l => l.Id != id);
            return Task.FromResult(q.Any());
        }

        public Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken ct = default)
        {
            var max = _lanes.Values.Where(l => l.ProjectId == projectId).Select(l => (int?)l.Order).DefaultIfEmpty(null).Max();
            return Task.FromResult(max ?? -1);
        }

        private static Lane Clone(Lane l)
        {
            var clone = Lane.Create(l.ProjectId, LaneName.Create(l.Name), l.Order);
            var rowVersion = (l.RowVersion is null) ? Array.Empty<byte>() : l.RowVersion.ToArray();
            clone.SetRowVersion(rowVersion);
            return clone;
        }

        private static byte[] NextRowVersion() => Guid.NewGuid().ToByteArray();
    }
}
