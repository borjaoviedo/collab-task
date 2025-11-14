using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
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

        /// <summary>
        /// Lists users ordered by name including their project memberships.
        /// </summary>
        public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .OrderBy(u => u.Name)
                        .Include(u => u.ProjectMemberships)
                        .ToListAsync(ct);

        /// <summary>
        /// Gets a user by email without tracking.
        /// </summary>
        public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == email, ct);

        /// <summary>
        /// Gets a user by id without tracking, including memberships.
        /// </summary>
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .Include(u => u.ProjectMemberships)
                        .FirstOrDefaultAsync(u => u.Id == id, ct);

        /// <summary>
        /// Gets a tracked user by id.
        /// </summary>
        public async Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        /// <summary>
        /// Adds a new user to the context.
        /// </summary>
        public async Task AddAsync(User item, CancellationToken ct = default)
            => await _db.Users.AddAsync(item, ct);

        /// <summary>
        /// Renames a user with optimistic concurrency and uniqueness check on name.
        /// </summary>
        public async Task<PrecheckStatus> RenameAsync(
            Guid id,
            UserName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return PrecheckStatus.NotFound;
            if (user.Name == newName) return PrecheckStatus.NoOp;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;

            // No-op if identical and conflict if name already used by another user.
            if (string.Equals(user.Name, newName, StringComparison.Ordinal))
                return PrecheckStatus.NoOp;

            if (await ExistsWithNameAsync(newName, user.Id, ct))
                return PrecheckStatus.Conflict;

            user.Rename(newName);
            _db.Entry(user).Property(u => u.Name).IsModified = true;

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Changes a user's role with optimistic concurrency.
        /// </summary>
        public async Task<PrecheckStatus> ChangeRoleAsync(
            Guid id,
            UserRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return PrecheckStatus.NotFound;
            if (user.Role == newRole) return PrecheckStatus.NoOp;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;

            // No-op guard already checked above.
            user.ChangeRole(newRole);
            _db.Entry(user).Property(u => u.Role).IsModified = true;

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Deletes a user with optimistic concurrency.
        /// </summary>
        public async Task<PrecheckStatus> DeleteAsync(
            Guid id,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return PrecheckStatus.NotFound;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;
            _db.Users.Remove(user);

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Checks if an email already exists, optionally excluding a user id.
        /// </summary>
        public async Task<bool> ExistsWithEmailAsync(
            Email email,
            Guid? excludeUserId = null,
            CancellationToken ct = default)
        {
            var q = _db.Users
                        .AsNoTracking()
                        .Where(u => u.Email == email);

            if (excludeUserId.HasValue)
                q = q.Where(u => u.Id != excludeUserId.Value);

            return await q.AnyAsync(ct);
        }

        /// <summary>
        /// Checks if a name already exists, optionally excluding a user id.
        /// </summary>
        public async Task<bool> ExistsWithNameAsync(
            UserName name,
            Guid? excludeUserId = null,
            CancellationToken ct = default)
        {
            var q = _db.Users
                        .AsNoTracking()
                        .Where(u => u.Name == name);

            if (excludeUserId.HasValue)
                q = q.Where(u => u.Id != excludeUserId.Value);

            return await q.AnyAsync(ct);
        }
    }
}
