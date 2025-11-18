using Application.Columns.DTOs;
using Domain.Entities;

namespace Application.Columns.Abstractions
{
    /// <summary>
    /// Provides read-only access to <see cref="Column"/> entities.
    /// </summary>
    public interface IColumnReadService
    {
        /// <summary>
        /// Retrieves a column by its unique identifier.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/>
        /// when the column does not exist or is not accessible to the current user.
        /// </summary>
        /// <param name="columnId">The unique identifier of the column to retrieve.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ColumnReadDto"/> representing the requested column.
        /// </returns>
        Task<ColumnReadDto> GetByIdAsync(Guid columnId, CancellationToken ct = default);

        /// <summary>
        /// Lists all columns belonging to the specified lane, ordered by their configured sort/order value.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the lane does not exist.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane whose columns will be listed.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="ColumnReadDto"/> objects for the specified lane.
        /// </returns>
        Task<IReadOnlyList<ColumnReadDto>> ListByLaneIdAsync(
            Guid laneId,
            CancellationToken ct = default);
    }
}
