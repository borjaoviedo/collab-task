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

        private static byte[] NextRowVersion() => Guid.NewGuid().ToByteArray();

        public Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default)
            => Task.FromResult(_lanes.TryGetValue(laneId, out var l) ? Clone(l) : null);

        public Task<Lane?> GetTrackedByIdAsync(Guid laneId, CancellationToken ct = default)
            => Task.FromResult(_lanes.TryGetValue(laneId, out var l) ? l : null);

        public Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Lane>>(_lanes.Values.Where(l => l.ProjectId == projectId)
                .OrderBy(l => l.Order).Select(Clone).ToList());

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

        public Task AddAsync(Lane lane, CancellationToken ct = default)
        {
            lock (_lock)
            {
                lane.RowVersion = NextRowVersion();
                _lanes[lane.Id] = lane;
            }
            return Task.CompletedTask;
        }

        public async Task<DomainMutation> RenameAsync(Guid laneId, LaneName newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return DomainMutation.NotFound;

            if (!lane.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;
            if (string.Equals(lane.Name, newName, StringComparison.Ordinal)) return DomainMutation.NoOp;

            if (await ExistsWithNameAsync(lane.ProjectId, newName, lane.Id, ct)) return DomainMutation.Conflict;

            lane.Rename(LaneName.Create(newName));
            lane.RowVersion = NextRowVersion();
            return DomainMutation.Updated;
        }

        public async Task<DomainMutation> ReorderAsync(Guid laneId, int newOrder, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return DomainMutation.NotFound;
            if (!lane.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;

            var lanes = _lanes.Values.Where(l => l.ProjectId == lane.ProjectId).OrderBy(l => l.Order).ToList();
            var currentIndex = lanes.FindIndex(l => l.Id == laneId);
            if (currentIndex < 0) return DomainMutation.NotFound;

            var targetIndex = Math.Clamp(newOrder, 0, lanes.Count - 1);
            if (currentIndex == targetIndex) return DomainMutation.NoOp;

            var moving = lanes[currentIndex];
            lanes.RemoveAt(currentIndex);
            lanes.Insert(targetIndex, moving);

            for (int i = 0; i < lanes.Count; i++)
            {
                if (lanes[i].Order != i)
                {
                    lanes[i].Reorder(i);
                    lanes[i].RowVersion = NextRowVersion();
                }
            }
            return DomainMutation.Updated;
        }

        public async Task<DomainMutation> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default)
        {
            var lane = await GetTrackedByIdAsync(laneId, ct);
            if (lane is null) return DomainMutation.NotFound;
            if (!lane.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;

            _lanes.Remove(laneId);
            return DomainMutation.Deleted;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        private static Lane Clone(Lane l)
        {
            var clone = Lane.Create(l.ProjectId, LaneName.Create(l.Name), l.Order);
            clone.RowVersion = (l.RowVersion is null) ? Array.Empty<byte>() : l.RowVersion.ToArray();
            return clone;
        }
    }
}
