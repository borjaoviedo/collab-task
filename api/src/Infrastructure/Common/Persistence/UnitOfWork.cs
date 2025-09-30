using Application.Common.Abstractions.Persistence;
using Infrastructure.Data;

namespace Infrastructure.Common.Persistence
{
    public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => db.SaveChangesAsync(ct);
    }
}
