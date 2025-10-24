using Application.Lanes.Abstractions;
using Domain.Entities;

namespace Application.Lanes.Services
{
    public sealed class LaneReadService(ILaneRepository repo) : ILaneReadService
    {
        public async Task<Lane?> GetAsync(Guid laneId, CancellationToken ct = default)
            => await repo.GetByIdAsync(laneId, ct);

        public async Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
            => await repo.ListByProjectAsync(projectId, ct);
    }
}
