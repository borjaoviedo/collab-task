using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Abstractions
{
    public interface IColumnWriteService
    {
        Task<(DomainMutation, Column?)> CreateAsync(Guid projectId, Guid laneId, ColumnName name, int? order = null, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid columnId, ColumnName newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> ReorderAsync(Guid columnId, int newOrder, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default);
    }
}
