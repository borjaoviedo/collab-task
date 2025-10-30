using Domain.Entities;

namespace Application.Columns.Abstractions
{
    /// <summary>
    /// Read-side operations for <see cref="Column"/> entities.
    /// </summary>
    public interface IColumnReadService
    {
        /// <summary>
        /// Retrieves a column by its unique identifier.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>The matching <see cref="Column"/> or <c>null</c> if not found.</returns>
        Task<Column?> GetAsync(Guid columnId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves all columns that belong to a given lane, ordered by their display order.
        /// </summary>
        /// <param name="laneId">The identifier of the parent lane.</param>
        /// <param name="ct">Optional cancellation token.</param>
        /// <returns>A read-only list of <see cref="Column"/> instances.</returns>
        Task<IReadOnlyList<Column>> ListByLaneAsync(Guid laneId, CancellationToken ct = default);
    }
}
