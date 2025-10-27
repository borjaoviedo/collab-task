using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class UserRepository(AppDbContext db) : IUserRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .OrderBy(u => u.Name)
                        .Include(u => u.ProjectMemberships)
                        .ToListAsync(ct);

        public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == email, ct);

        public async Task<User?> GetByNameAsync(UserName name, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Name == name, ct);

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .Include(u => u.ProjectMemberships)
                        .FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task AddAsync(User item, CancellationToken ct = default)
            => await _db.Users.AddAsync(item, ct);

        public async Task<PrecheckStatus> RenameAsync(Guid id, UserName newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return PrecheckStatus.NotFound;
            if (user.Name == newName) return PrecheckStatus.NoOp;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;

            // No-op check based on previous name
            if (string.Equals(user.Name, newName, StringComparison.Ordinal))
                return PrecheckStatus.NoOp;

            if (await ExistsWithNameAsync(newName, user.Id, ct))
                return PrecheckStatus.Conflict;

            user.Rename(newName);
            _db.Entry(user).Property(u => u.Name).IsModified = true;

            return PrecheckStatus.Ready;
        }

        public async Task<PrecheckStatus> ChangeRoleAsync(Guid id, UserRole newRole, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return PrecheckStatus.NotFound;
            if (user.Role == newRole) return PrecheckStatus.NoOp;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;

            // No-op check based on previous role
            if (user.Role == newRole) return PrecheckStatus.NoOp;

            user.ChangeRole(newRole);
            _db.Entry(user).Property(u => u.Role).IsModified = true;

            return PrecheckStatus.Ready;
        }

        public async Task<PrecheckStatus> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return PrecheckStatus.NotFound;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;
            _db.Users.Remove(user);

            return PrecheckStatus.Ready;
        }

        public async Task<bool> ExistsWithEmailAsync(Email email, Guid? excludeUserId = null, CancellationToken ct = default)
        {
            var q = _db.Users
                        .AsNoTracking()
                        .Where(u => u.Email == email);

            if (excludeUserId.HasValue)
                q = q.Where(u => u.Id != excludeUserId.Value);

            return await q.AnyAsync(ct);
        }

        public async Task<bool> ExistsWithNameAsync(UserName name, Guid? excludeUserId = null, CancellationToken ct = default)
        {
            var q = _db.Users
                        .AsNoTracking()
                        .Where(u => u.Name == name);

            if (excludeUserId.HasValue)
                q = q.Where(u => u.Id != excludeUserId.Value);

            return await q.AnyAsync(ct);
        }

        public async Task<bool> AnyAdminAsync(CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Role == UserRole.Admin, ct);

        public async Task<int> CountAdminsAsync(CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .CountAsync(u => u.Role == UserRole.Admin, ct);
    }
}
