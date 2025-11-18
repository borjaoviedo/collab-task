using Application.Columns.Abstractions;
using Application.Columns.DTOs;
using Application.Columns.Mapping;
using Application.Common.Exceptions;

namespace Application.Columns.Services
{
    /// <summary>
    /// Application read-side service for <see cref="Domain.Entities.Column"/> aggregates.
    /// Provides operations for retrieving individual columns or listing all columns
    /// belonging to a given lane. All returned results are mapped to
    /// <see cref="ColumnReadDto"/> representations. This service delegates storage
    /// concerns to <see cref="IColumnRepository"/> and ensures that missing resources
    /// are surfaced as <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="columnRepository">
    /// Repository used for querying <see cref="Domain.Entities.Column"/> entities,
    /// including lookups by identifier and lane association.
    /// </param>
    public sealed class ColumnReadService(
        IColumnRepository columnRepository) : IColumnReadService
    {
        private readonly IColumnRepository _columnRepository = columnRepository;

        /// <inheritdoc/>
        public async Task<ColumnReadDto> GetByIdAsync(
            Guid columnId,
            CancellationToken ct = default)
        {
            var column = await _columnRepository.GetByIdAsync(columnId, ct)
                // 404 if the column does not exist
                ?? throw new NotFoundException("Column not found.");

            return column.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ColumnReadDto>> ListByLaneIdAsync(
            Guid laneId,
            CancellationToken ct = default)
        {
            var columns = await _columnRepository.ListByLaneIdAsync(laneId, ct);

            return columns
                .Select(c => c.ToReadDto())
                .ToList();
        }
    }
}
