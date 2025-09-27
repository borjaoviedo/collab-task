using Application.Common.Abstractions.Time;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Data.Interceptors
{
    public sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IDateTimeProvider _clock;
        public AuditingSaveChangesInterceptor(IDateTimeProvider clock) => _clock = clock;

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            Stamp(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Stamp(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

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
                    // Ignora cambios solo en UpdatedAt/RowVersion para evitar UPDATE vacío
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
