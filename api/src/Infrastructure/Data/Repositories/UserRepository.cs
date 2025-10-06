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

        public async Task AddAsync(User item, CancellationToken ct = default)
            => await _db.Users.AddAsync(item, ct);

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

        public async Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null) return DomainMutation.NotFound;
            if (user.Name == newName) return DomainMutation.NoOp;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;

            user.Rename(UserName.Create(newName));
            _db.Entry(user).Property(u => u.Name).IsModified = true;

            return DomainMutation.Updated;
        }

        public async Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole role, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null) return DomainMutation.NotFound;
            if (user.Role == role) return DomainMutation.NoOp;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;

            user.ChangeRole(role);
            _db.Entry(user).Property(u => u.Role).IsModified = true;

            return DomainMutation.Updated;
        }

        public async Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null) return DomainMutation.NotFound;

            _db.Entry(user).Property(u => u.RowVersion).OriginalValue = rowVersion;
            _db.Users.Remove(user);

            return DomainMutation.Deleted;
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
