using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;
using System.Collections.Concurrent;

namespace Api.Tests.Fakes
{
    public sealed class FakeProjectMemberRepository : IProjectMemberRepository
    {
        // (ProjectId, UserId) -> ProjectMember (tracked instance)
        private readonly ConcurrentDictionary<(Guid, Guid), ProjectMember> _byKey = new();

        // UserId -> User (for Include(pm => pm.User) behavior)
        private readonly ConcurrentDictionary<Guid, User> _users = new();

        // simple rowversion counter
        private long _rv = 1;

        public Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
        {
            var key = (projectId, userId);
            if (_byKey.TryGetValue(key, out var pm))
                return Task.FromResult<ProjectMember?>(Clone(pm, includeUser: false));

            return Task.FromResult<ProjectMember?>(null);
        }

        public Task<ProjectMember?> GetTrackedByIdAsync(Guid projectId, Guid userId, CancellationToken ct = default)
        {
            _byKey.TryGetValue((projectId, userId), out var pm);
            // Return the tracked instance directly to simulate EF tracking.
            return Task.FromResult<ProjectMember?>(pm);
        }

        public Task<IReadOnlyList<ProjectMember>> GetByProjectAsync(Guid projectId, bool includeRemoved = false, CancellationToken ct = default)
        {
            IEnumerable<ProjectMember> q = _byKey.Values.Where(pm => pm.ProjectId == projectId);
            if (!includeRemoved) q = q.Where(pm => pm.RemovedAt is null);

            var list = q.Select(pm =>
            {
                // Simulate Include(pm => pm.User)
                _users.TryGetValue(pm.UserId, out var u);
                return Clone(pm, includeUser: true, user: u);
            }).ToList().AsReadOnly();

            return Task.FromResult<IReadOnlyList<ProjectMember>>(list);
        }

        public Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_byKey.ContainsKey((projectId, userId)));

        public Task<int> CountUserActiveMembershipsAsync(Guid userId, CancellationToken ct = default)
        {
            var count = _byKey.Values
                .Where(pm => pm.UserId == userId && pm.RemovedAt is null)
                .Select(pm => pm.ProjectId)
                .Distinct()
                .Count();

            return Task.FromResult(count);
        }

        public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
        {
            if (_byKey.TryGetValue((projectId, userId), out var pm))
                return Task.FromResult<ProjectRole?>(pm.Role);

            return Task.FromResult<ProjectRole?>(null);
        }

        public Task AddAsync(ProjectMember member, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(member);

            var key = (member.ProjectId, member.UserId);

            if (member.RowVersion is null || member.RowVersion.Length == 0)
                member.RowVersion = NextRowVersion();

            // keep a tracked instance internally
            var tracked = Clone(member, includeUser: false);
            if (!_byKey.TryAdd(key, tracked))
                throw new InvalidOperationException("Duplicate membership.");

            // cache user to simulate Include
            if (member.User is not null)
                _users[member.UserId] = Clone(member.User);

            return Task.CompletedTask;
        }

        public Task<DomainMutation> UpdateRoleAsync(Guid projectId, Guid userId, ProjectRole newRole, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(DomainMutation.Conflict);

            var key = (projectId, userId);
            if (!_byKey.TryGetValue(key, out var current) || current.RemovedAt is not null)
                return Task.FromResult(DomainMutation.NotFound);

            if (current.Role == newRole)
                return Task.FromResult(DomainMutation.NoOp);

            if (!RowVersionEquals(current.RowVersion, rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            current.ChangeRole(newRole);
            current.RowVersion = NextRowVersion();
            return Task.FromResult(DomainMutation.Updated);
        }

        public Task<DomainMutation> SetRemovedAsync(Guid projectId, Guid userId, DateTimeOffset? removedAt, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(DomainMutation.Conflict);

            var key = (projectId, userId);
            if (!_byKey.TryGetValue(key, out var current))
                return Task.FromResult(DomainMutation.NotFound);

            if (current.RemovedAt == removedAt)
                return Task.FromResult(DomainMutation.NoOp);

            if (!RowVersionEquals(current.RowVersion, rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            current.Remove(removedAt);
            current.RowVersion = NextRowVersion();
            return Task.FromResult(DomainMutation.Updated);
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        // ----------------- helpers -----------------

        private static bool RowVersionEquals(byte[] a, byte[] b)
            => a.Length == b.Length && a.SequenceEqual(b);

        private byte[] NextRowVersion()
            => BitConverter.GetBytes(Interlocked.Increment(ref _rv));

        private static ProjectMember Clone(ProjectMember m, bool includeUser, User? user = null)
        {
            var clone = ProjectMember.Create(m.ProjectId, m.UserId, m.Role);
            clone.RowVersion = (m.RowVersion is null) ? [] : m.RowVersion.ToArray();
            clone.Remove(m.RemovedAt);

            if (includeUser && user is not null)
                clone.User = Clone(user);

            return clone;
        }

        private static User Clone(User u)
        {
            var clone = User.Create(u.Email, u.Name, u.PasswordHash, u.PasswordSalt, u.Role);
            clone.RowVersion = (u.RowVersion is null) ? [] : u.RowVersion.ToArray();
            return clone;
        }
    }
}
