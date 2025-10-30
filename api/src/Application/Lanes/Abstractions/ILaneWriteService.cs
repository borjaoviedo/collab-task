using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Lanes.Abstractions
{
    /// <summary>
    /// Handles lane creation and mutation commands at the application level.
    /// </summary>
    public interface ILaneWriteService
    {
        /// <summary>Creates a new lane within the given project.</summary>
        Task<(DomainMutation, Lane?)> CreateAsync(
            Guid projectId,
            LaneName name,
            int? order = null,
            CancellationToken ct = default);

        /// <summary>Renames an existing lane.</summary>
        Task<DomainMutation> RenameAsync(
            Guid laneId,
            LaneName newName,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Changes the display order of a lane.</summary>
        Task<DomainMutation> ReorderAsync(
            Guid laneId,
            int newOrder,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes an existing lane.</summary>
        Task<DomainMutation> DeleteAsync(
            Guid laneId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
