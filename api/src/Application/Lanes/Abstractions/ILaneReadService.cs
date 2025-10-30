using Domain.Entities;

namespace Application.Lanes.Abstractions
{
    /// <summary>
    /// Provides read-only access to lanes within a project board.
    /// </summary>
    public interface ILaneReadService
    {
        /// <summary>
        /// Retrieves a lane by its unique identifier.
        /// </summary>
        Task<Lane?> GetAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>
        /// Lists all lanes that belong to the specified project.
        /// </summary>
        Task<IReadOnlyList<Lane>> ListByProjectAsync(Guid projectId, CancellationToken ct = default);
    }
}
