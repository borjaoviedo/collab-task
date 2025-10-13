using Domain.Entities;
using Domain.Enums;

namespace Application.Lanes.Abstractions
{
    public interface ILaneWriteService
    {
        Task<(DomainMutation, Lane?)> CreateAsync(Guid projectId, string name, int? order = null, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid laneId, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> ReorderAsync(Guid laneId, int newOrder, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid laneId, byte[] rowVersion, CancellationToken ct = default);
    }
}
