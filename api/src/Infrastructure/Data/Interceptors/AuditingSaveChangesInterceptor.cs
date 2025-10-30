using Application.Common.Abstractions.Time;
using Domain.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Intercepts EF Core save operations to automatically apply audit timestamps
    /// (<see cref="IAuditable.CreatedAt"/> and <see cref="IAuditable.UpdatedAt"/>) 
    /// using a provided <see cref="IDateTimeProvider"/>.
    /// </summary>
    public sealed class AuditingSaveChangesInterceptor(IDateTimeProvider clock) : SaveChangesInterceptor
    {
        private readonly IDateTimeProvider _clock = clock;

        /// <summary>
        /// Synchronously stamps audit fields before changes are persisted.
        /// </summary>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            Stamp(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        /// <summary>
        /// Asynchronously stamps audit fields before changes are persisted.
        /// </summary>
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Stamp(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>
        /// Updates <see cref="IAuditable"/> entities with creation and modification timestamps.
        /// Newly added entities receive both CreatedAt and UpdatedAt values.
        /// Modified entities update only the UpdatedAt value when real data changes exist.
        /// </summary>
        /// <param name="context">The current EF Core DbContext being tracked.</param>
        private void Stamp(DbContext? context)
        {
            if (context is null) return;
            var now = _clock.UtcNow;

            foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    continue;
                }

                if (entry.State == EntityState.Modified)
                {
                    var hasRealChanges = entry.Properties.Any(p =>
                        p.IsModified &&
                        p.Metadata.Name is not nameof(IAuditable.UpdatedAt) &&
                        p.Metadata.Name is not "RowVersion");

                    if (hasRealChanges)
                        entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    else
                        entry.State = EntityState.Unchanged;
                }
            }
        }
    }
}
