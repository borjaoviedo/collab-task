using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Abstractions
{
    public interface IColumnRepository
    {
        Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default);
        Task<Column?> GetByIdAsync(Guid columnId, CancellationToken ct = default);
        Task<Column?> GetTrackedByIdAsync(Guid columnId, CancellationToken ct = default);

        Task AddAsync(Column column, CancellationToken ct = default);

        Task<PrecheckStatus> RenameAsync(
            Guid columnId,
            ColumnName newName,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<PrecheckStatus> ReorderPhase1Async(
            Guid columnId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task ApplyReorderPhase2Async(Guid columnId, CancellationToken ct = default);
        Task<PrecheckStatus> DeleteAsync(
            Guid columnId,
            byte[] rowVersion,
            CancellationToken ct = default);

        Task<bool> ExistsWithNameAsync
            (Guid laneId,
            ColumnName name,
            Guid? excludeColumnId = null,
            CancellationToken ct = default);
        Task<int> GetMaxOrderAsync(Guid laneId, CancellationToken ct = default);
    }
}
