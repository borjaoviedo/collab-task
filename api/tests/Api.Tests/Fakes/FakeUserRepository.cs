using Application.Users.Abstractions;
using Domain.Common.Exceptions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using System.Collections.Concurrent;

namespace Api.Tests.Fakes
{
    public sealed class FakeUserRepository : IUserRepository
    {
        // Email -> User (case-insensitive)
        private readonly ConcurrentDictionary<string, User> _byEmail = new(StringComparer.OrdinalIgnoreCase);

        // Name -> User (case-insensitive)
        private readonly ConcurrentDictionary<string, User> _byName = new(StringComparer.OrdinalIgnoreCase);

        // Id -> Email
        private readonly ConcurrentDictionary<Guid, string> _byId = new();

        // simple rowversion counter
        private long _rv = 1;

        public Task AddAsync(User item, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(item);
            var email = item.Email.Value;

            if (!_byEmail.TryAdd(email, PrepareForInsert(item)))
                throw new DuplicateEntityException("Could not complete registration.");

            var name = item.Name.Value;

            if (!_byName.TryAdd(name, PrepareForInsert(item)))
                throw new DuplicateEntityException("Could not complete registration.");

            _byId[item.Id] = email;
            return Task.FromResult(item.Id);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            if (_byEmail.TryGetValue(email, out var u))
                return Task.FromResult<User?>(Clone(u));

            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByNameAsync(string name, CancellationToken ct = default)
        {
            if (_byName.TryGetValue(name, out var u))
                return Task.FromResult<User?>(Clone(u));

            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_byId.TryGetValue(id, out var email) && _byEmail.TryGetValue(email, out var u))
                return Task.FromResult<User?>(Clone(u));

            return Task.FromResult<User?>(null);
        }

        public Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(DomainMutation.Conflict);

            if (!_byId.TryGetValue(id, out var email))
                return Task.FromResult(DomainMutation.NotFound);

            if (!_byEmail.TryGetValue(email, out var current))
                return Task.FromResult(DomainMutation.NotFound);

            if (!RowVersionEquals(current.RowVersion, rowVersion) || current.Name == UserName.Create(newName))
                return Task.FromResult(DomainMutation.NoOp);

            var updated = Clone(current);
            updated.Name = UserName.Create(newName);
            updated.RowVersion = NextRowVersion();

            _byEmail[email] = updated;

            return Task.FromResult(DomainMutation.Updated);
        }

        public Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole role, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(DomainMutation.Conflict);

            if (!_byId.TryGetValue(id, out var email))
                return Task.FromResult(DomainMutation.NotFound);

            if (!_byEmail.TryGetValue(email, out var current))
                return Task.FromResult(DomainMutation.NotFound);

            if (!RowVersionEquals(current.RowVersion, rowVersion) || current.Role == role)
                return Task.FromResult(DomainMutation.NoOp);

            var updated = Clone(current);
            updated.Role = role;
            updated.RowVersion = NextRowVersion();

            _byEmail[email] = updated;
            return Task.FromResult(DomainMutation.Updated);
        }

        public Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(DomainMutation.Conflict);

            if (!_byId.TryGetValue(id, out var email))
                return Task.FromResult(DomainMutation.NotFound);

            if (!_byEmail.TryGetValue(email, out var current))
                return Task.FromResult(DomainMutation.NotFound);

            if (!RowVersionEquals(current.RowVersion, rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            _byEmail.TryRemove(email, out _);
            _byName.TryRemove(current.Name.Value, out _);
            _byId.TryRemove(id, out _);
            return Task.FromResult(DomainMutation.Deleted);
        }

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult(_byEmail.ContainsKey(email));

        public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
            => Task.FromResult(_byName.ContainsKey(name));

        public Task<bool> AnyAdminAsync(CancellationToken ct = default)
            => Task.FromResult(_byEmail.Values.Any(u => u.Role == UserRole.Admin));

        public Task<int> CountAdminsAsync(CancellationToken ct = default)
            => Task.FromResult(_byEmail.Values.Count(u => u.Role == UserRole.Admin));

        // ----------------- helpers -----------------

        private User PrepareForInsert(User u)
        {
            if (u.RowVersion is null || u.RowVersion.Length == 0)
                u.RowVersion = NextRowVersion();
            return Clone(u);
        }

        private static bool RowVersionEquals(byte[] a, byte[] b)
            => a.Length == b.Length && a.SequenceEqual(b);

        private byte[] NextRowVersion()
            => BitConverter.GetBytes(Interlocked.Increment(ref _rv));

        private static User Clone(User u)
        {
            var clone = User.Create(u.Email, u.Name, u.PasswordHash.ToArray(), u.PasswordSalt.ToArray(), u.Role);
            clone.Id = u.Id;
            clone.CreatedAt = u.CreatedAt;
            clone.UpdatedAt = u.UpdatedAt;
            clone.RowVersion = (u.RowVersion is null) ? Array.Empty<byte>() : u.RowVersion.ToArray();
            return clone;
        }
    }
}
