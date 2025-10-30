using Application.Lanes.Abstractions;
using Domain.Entities;

namespace Application.Lanes.Services
{
    /// <summary>
    /// Read-only application service for lanes.
    /// </summary>
    public sealed class LaneReadService(ILaneRepository repo) : ILaneReadService
    {
        /// <summary>
        /// Retrieves a lane by identifier.
        /// </summary>
        /// <param name="laneId">The lane identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The lane if found; otherwise <c>null</c>.</returns>
        public async Task<Lane?> GetAsync(Guid laneId, CancellationToken ct = default)
            => await repo.GetByIdAsync(laneId, ct);

        /// <summary>
        /// Lists all lanes for a project ordered by display order.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A read-only list of lanes.</returns>
        public async Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default)
            => await repo.ListByProjectAsync(projectId, ct);
    }

}
