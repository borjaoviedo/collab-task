using Domain.Entities;
using Domain.Enums;

namespace Application.Columns.Abstractions
{
    public interface IColumnRepository
    {
        Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default);
        Task<Column?> GetTrackedByIdAsync(Guid columnId, CancellationToken ct = default);
        Task<bool> ExistsWithNameAsync(Guid laneId, string name, Guid? excludeColumnId = null, CancellationToken ct = default);
        Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default);
        Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default);

        Task AddAsync(Column column, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid columnId, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> ReorderAsync(Guid columnId, int newOrder, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid columnId, byte[] rowVersion, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
