using Application.Abstractions.Persistence;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Coordinates persistence of changes and translates EF Core outcomes
    /// into high-level <see cref="DomainMutation"/> results.
    /// </summary>
    public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
    {
        /// <summary>
        /// Persists pending changes and maps the operation kind to a domain-level mutation result.
        /// Converts concurrency and delete-related database errors into <see cref="DomainMutation.Conflict"/>.
        /// </summary>
        /// <param name="kind">The semantic kind of mutation being performed.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="DomainMutation"/> indicating the outcome:
        /// <see cref="DomainMutation.Created"/>, <see cref="DomainMutation.Updated"/>,
        /// <see cref="DomainMutation.Deleted"/>, <see cref="DomainMutation.NoOp"/>, or
        /// <see cref="DomainMutation.Conflict"/> on concurrency or FK violations.
        /// </returns>
        public async Task<DomainMutation> SaveAsync(MutationKind kind, CancellationToken ct = default)
        {
            try
            {
                // Flush pending tracked changes
                await db.SaveChangesAsync(ct);

                // Map the requested mutation kind to the final domain mutation result
                return kind switch
                {
                    MutationKind.Create => DomainMutation.Created,
                    MutationKind.Update => DomainMutation.Updated,
                    MutationKind.Delete => DomainMutation.Deleted,
                    _ => DomainMutation.NoOp
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                // Optimistic concurrency failure (rowversion/ETag mismatch)
                return DomainMutation.Conflict;
            }
            catch (DbUpdateException) when (kind == MutationKind.Delete)
            {
                // Typical case: FK restriction prevents delete
                return DomainMutation.Conflict;
            }
        }
    }
}
