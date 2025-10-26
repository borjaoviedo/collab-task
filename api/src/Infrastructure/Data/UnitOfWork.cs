using Application.Common.Abstractions.Persistence;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
    {
        public async Task<DomainMutation> SaveAsync(MutationKind kind, CancellationToken ct = default)
        {
            try
            {
                await db.SaveChangesAsync(ct);
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
                return DomainMutation.Conflict;
            }
            catch (DbUpdateException) when (kind == MutationKind.Delete)
            {
                return DomainMutation.Conflict;
            }
        }
    }
}
