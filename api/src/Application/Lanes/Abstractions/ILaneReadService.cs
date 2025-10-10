using Domain.Entities;

namespace Application.Lanes.Abstractions
{
    public interface ILaneReadService
    {
        Task<Lane?> GetAsync(Guid laneId, CancellationToken ct = default);
        Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
    }
}
