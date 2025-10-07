using Application.Common.Abstractions.Persistence;
using Application.Common.Exceptions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public UnitOfWork(AppDbContext db) => _db = db;

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                return await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("Optimistic concurrency conflict.");
            }
        }
    }
}
