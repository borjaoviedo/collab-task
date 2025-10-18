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

        public async Task AddAsync(User item, CancellationToken ct = default)
            => await _db.Users.AddAsync(item, ct);

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .OrderBy(u => u.Name)
                        .Include(u => u.ProjectMemberships)
                        .ToListAsync(ct);

        public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Email == Email.Create(email), ct);

        public async Task<User?> GetByNameAsync(string name, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Name == UserName.Create(name), ct);

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Users
                        .AsNoTracking()
                        .Include(u => u.ProjectMemberships)
                        .FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return DomainMutation.NotFound;
            if (user.Name == newName) return DomainMutation.NoOp;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;

            // No-op check based on previous name
            var before = user.Name;
            if (string.Equals(before, newName, StringComparison.Ordinal))
                return DomainMutation.NoOp;

            if (await ExistsWithNameAsync(newName, user.Id, ct))
                return DomainMutation.Conflict;

            user.Rename(UserName.Create(newName));
            _db.Entry(user).Property(u => u.Name).IsModified = true;

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Updated;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole role, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return DomainMutation.NotFound;
            if (user.Role == role) return DomainMutation.NoOp;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;

            // No-op check based on previous role
            var before = user.Role;
            if (before == role)
                return DomainMutation.NoOp;

            user.ChangeRole(role);
            _db.Entry(user).Property(u => u.Role).IsModified = true;

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Updated;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await GetTrackedByIdAsync(id, ct);
            if (user is null) return DomainMutation.NotFound;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;
            _db.Users.Remove(user);

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Deleted;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
            catch (DbUpdateException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<bool> ExistsWithEmailAsync(string email, Guid? excludeUserId = null, CancellationToken ct = default)
        {
            var q = _db.Users.AsNoTracking().Where(u => u.Email == email);
            if (excludeUserId.HasValue) q = q.Where(u => u.Id != excludeUserId.Value);
            return await q.AnyAsync(ct);
        }

        public async Task<bool> ExistsWithNameAsync(string name, Guid? excludeUserId = null, CancellationToken ct = default)
        {
            var q = _db.Users.AsNoTracking().Where(u => u.Name == name);
            if (excludeUserId.HasValue) q = q.Where(u => u.Id != excludeUserId.Value);
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

        public async Task<int> SaveChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);
    }
}
