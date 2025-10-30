using Application.Columns.Abstractions;
using Domain.Entities;

namespace Application.Columns.Services
{
    /// <summary>
    /// Read-only application service for columns.
    /// </summary>
    public sealed class ColumnReadService(IColumnRepository repo) : IColumnReadService
    {
        /// <summary>
        /// Retrieves a column by identifier.
        /// </summary>
        /// <param name="columnId">The column identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The column if found; otherwise <c>null</c>.</returns>
        public async Task<Column?> GetAsync(Guid columnId, CancellationToken ct = default)
            => await repo.GetByIdAsync(columnId, ct);

        /// <summary>
        /// Lists all columns that belong to the specified lane.
        /// </summary>
        /// <param name="laneId">The lane identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A read-only list of columns ordered by display order.</returns>
        public async Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default)
            => await repo.ListByLaneAsync(laneId, ct);
    }
}
