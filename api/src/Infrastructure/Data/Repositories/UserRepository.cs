using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        public UserRepository(AppDbContext db) => _db = db;

        public async Task<Guid> CreateAsync(User item, CancellationToken ct = default)
        {
            await _db.Users.AddAsync(item, ct);
            return item.Id;
        }

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
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> SetRoleAsync(Guid id, UserRole role, byte[] rowVersion, CancellationToken ct = default)
        {
            var updated = await _db.Users
                .Where(u => u.Id == id && u.RowVersion == rowVersion && u.Role != role)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, role), ct);

            return updated == 0 ? null
                : await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0) return false;

            var affected = await _db.Users
                .Where(u => u.Id == id && u.RowVersion == rowVersion)
                .ExecuteDeleteAsync(ct);

            return affected > 0;
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
            => await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email == Email.Create(email), ct);

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
            => await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Name == UserName.Create(name), ct);

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
