using Application.Lanes.Abstractions;
using Domain.Entities;

namespace Application.Lanes.Services
{
    public sealed class LaneReadService(ILaneRepository repo) : ILaneReadService
    {
        public Task<Lane?> GetAsync(Guid laneId, CancellationToken ct = default)
            => repo.GetByIdAsync(laneId, ct);

        public Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
            => repo.ListByProjectAsync(projectId, ct);
    }
}
