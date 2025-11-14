using Domain.Enums;

namespace Application.Abstractions.Persistence
{
    /// <summary>
    /// Coordinates the persistence boundary for a unit of work.
    /// </summary>
    /// <remarks>
    /// Call once per application command to flush pending changes. The returned <see cref="DomainMutation"/>
    /// must reflect the effective result of the operation described by <paramref name="kind"/>.
    /// </remarks>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Persists all pending changes as a single atomic operation.
        /// </summary>
        /// <param name="kind">The intended mutation kind (e.g., create, update, delete) for telemetry and mapping.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A <see cref="DomainMutation"/> describing the actual outcome after persistence,
        /// including no-op or concurrency-precondition failures where applicable.
        /// </returns>
        Task<DomainMutation> SaveAsync(MutationKind kind, CancellationToken ct = default);
    }
}
