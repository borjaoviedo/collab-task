using Application.Lanes.DTOs;

namespace Application.Lanes.Abstractions
{
    /// <summary>
    /// Provides read-only access to <see cref="Domain.Entities.Lane"/> entities.
    /// </summary>
    public interface ILaneReadService
    {
        /// <summary>
        /// Retrieves a lane by its unique identifier.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane to retrieve.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="LaneReadDto"/> representing the requested lane.
        /// </returns>
        Task<LaneReadDto> GetByIdAsync(Guid laneId, CancellationToken ct = default);

        /// <summary>
        /// Lists all lanes belonging to the specified project, ordered by their configured sort/order value.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the project does not exist.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project whose lanes will be listed.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="LaneReadDto"/> objects for the specified project.
        /// </returns>
        Task<IReadOnlyList<LaneReadDto>> ListByProjectIdAsync(Guid projectId, CancellationToken ct = default);
    }
}
