using Application.Common.Exceptions;
using Application.Lanes.Abstractions;
using Application.Lanes.DTOs;
using Application.Lanes.Mapping;

namespace Application.Lanes.Services
{
    /// <summary>
    /// Application read-side service for <see cref="Domain.Entities.Lane"/> aggregates.
    /// Provides operations for retrieving individual lanes or listing all lanes
    /// belonging to a given project. All returned results are mapped to
    /// <see cref="LaneReadDto"/> representations. This service delegates storage
    /// concerns to <see cref="ILaneRepository"/> and ensures that missing resources
    /// are surfaced as <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="laneRepository">
    /// Repository used for querying <see cref="Domain.Entities.Lane"/> entities,
    /// including lookups by identifier and project association.
    /// </param>
    public sealed class LaneReadService(
        ILaneRepository laneRepository) : ILaneReadService
    {
        private readonly ILaneRepository _laneRepository = laneRepository;

        /// <inheritdoc/>
        public async Task<LaneReadDto> GetByIdAsync(Guid laneId, CancellationToken ct = default)
        {
            var lane = await _laneRepository.GetByIdAsync(laneId, ct)
                // 404 if the lane does not exist
                ?? throw new NotFoundException("Lane not found.");

            return lane.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<LaneReadDto>> ListByProjectIdAsync(
            Guid projectId,
            CancellationToken ct = default)
        {
            var lanes = await _laneRepository.ListByProjectIdAsync(projectId, ct);

            return lanes
                .Select(l => l.ToReadDto())
                .ToList();
        }
    }

}
