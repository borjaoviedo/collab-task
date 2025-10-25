using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using System.Collections.Concurrent;

namespace Api.Tests.Fakes
{
    public sealed class FakeUserRepository : IUserRepository
    {
        // Case-insensitive indexes
        private readonly ConcurrentDictionary<Guid, User> _byId = new();
        private readonly ConcurrentDictionary<string, Guid> _idByEmail = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, Guid> _idByName = new(StringComparer.OrdinalIgnoreCase);

        // simple rowversion counter
        private long _rv = 1;

        public Task AddAsync(User item, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.RowVersion is null || item.RowVersion.Length == 0)
                item.SetRowVersion(NextRowVersion());

            _byId[item.Id] = item;
            _idByEmail[item.Email.Value] = item.Id;
            _idByName[item.Name.Value] = item.Id;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        {
            var list = _byId.Values
                .OrderBy(u => u.Name.Value)
                .ToList()
                .AsReadOnly();

            return Task.FromResult((IReadOnlyList<User>)list);
        }

        public Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
        {
            if (email is null) return Task.FromResult<User?>(null);
            return Task.FromResult(TryGetByEmail(email, out var u) ? u : null);
        }

        public Task<User?> GetByNameAsync(UserName name, CancellationToken ct = default)
        {
            if (name is null) return Task.FromResult<User?>(null);
            return Task.FromResult(TryGetByName(name, out var u) ? u : null);
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_byId.TryGetValue(id, out var u) ? u : null);

        public Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_byId.TryGetValue(id, out var u) ? u : null);

        public Task<DomainMutation> RenameAsync(Guid id, UserName newName, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0) return Task.FromResult(DomainMutation.Conflict);
            if (!_byId.TryGetValue(id, out var user)) return Task.FromResult(DomainMutation.NotFound);

            if (string.Equals(user.Name.Value, newName, StringComparison.Ordinal))
                return Task.FromResult(DomainMutation.NoOp);

            if (_idByName.TryGetValue(newName, out var otherId) && otherId != id)
                return Task.FromResult(DomainMutation.Conflict);

            if (!RowVersionEquals(user.RowVersion, rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            _idByName.TryRemove(user.Name.Value, out _);
            user.Rename(UserName.Create(newName));
            user.SetRowVersion(NextRowVersion());
            _idByName[user.Name.Value] = id;

            return Task.FromResult(DomainMutation.Updated);
        }

        public Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole role, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0) return Task.FromResult(DomainMutation.Conflict);
            if (!_byId.TryGetValue(id, out var user)) return Task.FromResult(DomainMutation.NotFound);

            if (user.Role == role) return Task.FromResult(DomainMutation.NoOp);

            if (!RowVersionEquals(user.RowVersion, rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            user.ChangeRole(role);
            user.SetRowVersion(NextRowVersion());

            return Task.FromResult(DomainMutation.Updated);
        }

        public Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0) return Task.FromResult(DomainMutation.Conflict);
            if (!_byId.TryGetValue(id, out var user)) return Task.FromResult(DomainMutation.NotFound);

            if (!RowVersionEquals(user.RowVersion, rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            _byId.TryRemove(id, out _);
            _idByEmail.TryRemove(user.Email.Value, out _);
            _idByName.TryRemove(user.Name.Value, out _);

            return Task.FromResult(DomainMutation.Deleted);
        }

        public Task<bool> ExistsWithEmailAsync(Email email, Guid? excludeUserId = null, CancellationToken ct = default)
        {
            if (email is null) return Task.FromResult(false);
            var exists = _idByEmail.TryGetValue(email, out var id);
            if (!exists) return Task.FromResult(false);
            return Task.FromResult(!excludeUserId.HasValue || excludeUserId.Value != id);
        }

        public Task<bool> ExistsWithNameAsync(UserName name, Guid? excludeUserId = null, CancellationToken ct = default)
        {
            if (name is null) return Task.FromResult(false);
            var exists = _idByName.TryGetValue(name, out var id);
            if (!exists) return Task.FromResult(false);
            return Task.FromResult(!excludeUserId.HasValue || excludeUserId.Value != id);
        }

        public Task<bool> AnyAdminAsync(CancellationToken ct = default)
            => Task.FromResult(_byId.Values.Any(u => u.Role == UserRole.Admin));

        public Task<int> CountAdminsAsync(CancellationToken ct = default)
            => Task.FromResult(_byId.Values.Count(u => u.Role == UserRole.Admin));

        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => Task.FromResult(0); // No-op

        // ----------------- helpers -----------------
        private static bool RowVersionEquals(byte[] a, byte[] b)
            => a is not null && b is not null && a.Length == b.Length && a.SequenceEqual(b);

        private byte[] NextRowVersion()
            => BitConverter.GetBytes(Interlocked.Increment(ref _rv));

        private bool TryGetByEmail(string email, out User user)
        {
            user = default!;
            if (_idByEmail.TryGetValue(email, out var id) && _byId.TryGetValue(id, out var found))
            {
                user = found;
                return true;
            }
            return false;
        }

        private bool TryGetByName(string name, out User user)
        {
            user = default!;
            if (_idByName.TryGetValue(name, out var id) && _byId.TryGetValue(id, out var found))
            {
                user = found;
                return true;
            }
            return false;
        }
    }
}
