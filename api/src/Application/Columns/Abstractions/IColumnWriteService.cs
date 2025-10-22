using Domain.Entities;
using Domain.Enums;

namespace Application.Columns.Abstractions
{
    public interface IColumnWriteService
    {
        Task<(DomainMutation, Column?)> CreateAsync(Guid projectId, Guid laneId, string name, int? order = null, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid columnId, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> ReorderAsync(Guid columnId, int newOrder, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default);
    }
}
