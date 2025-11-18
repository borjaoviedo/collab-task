using Application.Lanes.DTOs;

namespace Application.Lanes.Abstractions
{
    /// <summary>
    /// Provides write operations for managing <see cref="Domain.Entities.Lane"/> entities,
    /// including creation, renaming, reordering, and deletion within a project.
    /// All operations enforce domain invariants, naming uniqueness,
    /// board-structure integrity, and optimistic concurrency where applicable.
    /// </summary>
    public interface ILaneWriteService
    {
        /// <summary>
        /// Creates a new lane within the specified project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the parent project.</param>
        /// <param name="dto">The data required to create the lane.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="LaneReadDto"/> describing the newly created lane.
        /// </returns>
        Task<LaneReadDto> CreateAsync(
            Guid projectId,
            LaneCreateDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Renames an existing lane.
        /// Throws <see cref="Common.Exceptions.ConflictException"/> when a name conflict occurs
        /// or when concurrency validation fails.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane to rename.</param>
        /// <param name="dto">The new name and associated metadata.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="LaneReadDto"/> representing the updated lane.
        /// </returns>
        Task<LaneReadDto> RenameAsync(
            Guid laneId,
            LaneRenameDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Updates the display order of a lane within its project.
        /// Performs validation on position constraints and lane consistency
        /// before applying changes.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane to reorder.</param>
        /// <param name="dto">The new ordering information.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="LaneReadDto"/> representing the reordered lane.
        /// </returns>
        Task<LaneReadDto> ReorderAsync(
            Guid laneId,
            LaneReorderDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes a lane from its project.
        /// Enforces concurrency checks and validates that board structure
        /// remains consistent after deletion.
        /// </summary>
        /// <param name="laneId">The unique identifier of the lane to delete.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteByIdAsync(
            Guid laneId,
            CancellationToken ct = default);
    }
}
