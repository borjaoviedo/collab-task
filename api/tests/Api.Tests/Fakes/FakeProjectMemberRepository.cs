using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Tests.Fakes
{
    public sealed class FakeProjectMemberRepository : IProjectMemberRepository
    {
        // (ProjectId, UserId) -> ProjectMember
        private readonly ConcurrentDictionary<(Guid, Guid), ProjectMember> _byKey = new();

        // simple rowversion counter
        private long _rv = 1;

        public Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
        {
            var key = (projectId, userId);
            if (_byKey.TryGetValue(key, out var pm))
                return Task.FromResult<ProjectMember?>(Clone(pm));

            return Task.FromResult<ProjectMember?>(null);
        }

        public Task<IReadOnlyList<ProjectMember>> GetByProjectAsync(Guid projectId, bool includeRemoved = false, CancellationToken ct = default)
        {
            IEnumerable<ProjectMember> q = _byKey.Values.Where(pm => pm.ProjectId == projectId);
            if (!includeRemoved) q = q.Where(pm => pm.RemovedAt is null);

            var list = q.Select(Clone).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<ProjectMember>>(list);
        }

        public Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => Task.FromResult(_byKey.ContainsKey((projectId, userId)));

        public Task AddAsync(ProjectMember member, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(member);
            var key = (member.ProjectId, member.UserId);

            if (member.RowVersion is null || member.RowVersion.Length == 0)
                member.RowVersion = NextRowVersion();

            if (!_byKey.TryAdd(key, Clone(member)))
                throw new InvalidOperationException("Duplicate membership.");

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

            var updated = Clone(current);
            updated.ChangeRole(newRole);
            updated.RowVersion = NextRowVersion();

            _byKey[key] = updated;
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

            var updated = Clone(current);
            updated.Remove(removedAt);
            updated.RowVersion = NextRowVersion();

            _byKey[key] = updated;
            return Task.FromResult(DomainMutation.Updated);
        }

        // ----------------- helpers -----------------

        private static bool RowVersionEquals(byte[] a, byte[] b)
            => a.Length == b.Length && a.SequenceEqual(b);

        private byte[] NextRowVersion()
            => BitConverter.GetBytes(Interlocked.Increment(ref _rv));

        private static ProjectMember Clone(ProjectMember m)
        {
            var clone = ProjectMember.Create(m.ProjectId, m.UserId, m.Role, m.JoinedAt);
            clone.RowVersion = (m.RowVersion is null) ? Array.Empty<byte>() : m.RowVersion.ToArray();
            return clone;
        }
    }
}
