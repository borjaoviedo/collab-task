using Domain.Entities;
using Domain.Enums;

namespace Application.Lanes.Abstractions
{
    public interface ILaneRepository
    {
        Task<Lane?> GetByIdAsync(Guid laneId, CancellationToken ct = default);
        Task<Lane?> GetTrackedByIdAsync(Guid laneId, CancellationToken ct = default);
        Task<bool> ExistsWithNameAsync(Guid projectId, string name, Guid? excludeLaneId = null, CancellationToken ct = default);
        Task<int> GetMaxOrderAsync(Guid projectId, CancellationToken ct = default);
        Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);

        Task AddAsync(Lane lane, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid laneId, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> ReorderAsync(Guid laneId, int newOrder, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
