using Domain.Entities;

namespace Application.Columns.Abstractions
{
    public interface IColumnReadService
    {
        Task<Column?> GetAsync(Guid columnId, CancellationToken ct = default);
        Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default);
    }
}
