using Application.Columns.Abstractions;
using Domain.Entities;

namespace Application.Columns.Services
{
    public sealed class ColumnReadService(IColumnRepository repo) : IColumnReadService
    {
        public async Task<Column?> GetAsync(Guid columnId, CancellationToken ct = default)
            => await repo.GetByIdAsync(columnId, ct);
        public async Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default)
            => await repo.ListByLaneAsync(laneId, ct);
    }
}
