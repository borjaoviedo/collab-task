using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Columns.Abstractions
{
    /// <summary>
    /// Write-side operations for <see cref="Column"/> entities.
    /// </summary>
    public interface IColumnWriteService
    {
        /// <summary>
        /// Creates a new column within a lane.
        /// </summary>
        /// <param name="projectId">Parent project identifier.</param>
        /// <param name="laneId">Parent lane identifier.</param>
        /// <param name="name">Column name value object.</param>
        /// <param name="order">Optional order index; if null, appended at the end.</param>
        /// <param name="ct">Optional cancellation token.</param>
        Task<(DomainMutation, Column?)> CreateAsync(
            Guid projectId,
            Guid laneId,
            ColumnName name,
            int? order = null,
            CancellationToken ct = default);

        /// <summary>
        /// Renames a column if concurrency and naming constraints are satisfied.
        /// </summary>
        Task<DomainMutation> RenameAsync(
            Guid columnId,
            ColumnName newName,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Changes the order of a column within its lane, performing validation before persistence.
        /// </summary>
        Task<DomainMutation> ReorderAsync(
            Guid columnId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes a column, enforcing concurrency validation and board integrity checks.
        /// </summary>
        Task<DomainMutation> DeleteAsync(
            Guid columnId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
