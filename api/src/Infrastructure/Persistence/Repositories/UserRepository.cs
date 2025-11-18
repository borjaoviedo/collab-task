using Application.Users.Abstractions;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="User"/> aggregates.
    /// Supports listing, lookup by email/id, tracked fetch, rename and role change with concurrency,
    /// deletion, and uniqueness checks for email and name.
    /// </summary>
    public sealed class UserRepository(CollabTaskDbContext db) : IUserRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .OrderBy(u => u.Name)
                        .Include(u => u.ProjectMemberships)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == email, ct);

        /// <inheritdoc/>
        public async Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .Include(u => u.ProjectMemberships)
                        .FirstOrDefaultAsync(u => u.Id == userId, ct);

        /// <inheritdoc/>
        public Task<User?> GetByIdForUpdateAsync(Guid userId, CancellationToken ct = default)
            => _db.Users
                  .Include(u => u.ProjectMemberships)
                  .FirstOrDefaultAsync(u => u.Id == userId, ct);

        /// <inheritdoc/>
        public async Task AddAsync(User user, CancellationToken ct = default)
            => await _db.Users.AddAsync(user, ct);

        /// <inheritdoc/>
        public async Task UpdateAsync(User user, CancellationToken ct = default)
        {
            // If entity is already tracked, do nothing so EF change tracking produces minimal UPDATEs
            if (_db.Entry(user).State == EntityState.Detached)
                _db.Users.Update(user);

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(User user, CancellationToken ct = default)
        {
            // Mark entity as deleted; actual deletion occurs in UnitOfWork.SaveAsync()
            _db.Users.Remove(user);
            await Task.CompletedTask;
        }
    }
}
