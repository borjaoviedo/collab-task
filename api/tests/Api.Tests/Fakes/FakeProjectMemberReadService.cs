using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;
using System.Collections.Concurrent;

namespace Api.Tests.Fakes
{
    public sealed class FakeProjectMemberReadService : IProjectMemberReadService
    {
        private readonly ConcurrentDictionary<(Guid ProjectId, Guid UserId), ProjectMember> _byKey = new();
        private readonly Func<Guid, Guid, ProjectRole?>? _roleSelector;
        private readonly ProjectRole? _fixedRole;

        public FakeProjectMemberReadService() { }

        public FakeProjectMemberReadService(ProjectRole? fixedRole) => _fixedRole = fixedRole;

        public FakeProjectMemberReadService(Func<Guid, Guid, ProjectRole?> roleSelector) => _roleSelector = roleSelector;

        // Optional helper to seed memberships in tests
        public void Seed(ProjectMember member)
        {
            ArgumentNullException.ThrowIfNull(member);
            _byKey[(member.ProjectId, member.UserId)] = Clone(member);
        }

        public Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
        {
            if (_byKey.TryGetValue((projectId, userId), out var pm))
                return Task.FromResult<ProjectMember?>(Clone(pm));

            return Task.FromResult<ProjectMember?>(null);
        }

        public Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(Guid projectId, bool includeRemoved = false, CancellationToken ct = default)
        {
            var list = _byKey.Values
                .Where(pm => pm.ProjectId == projectId && (includeRemoved || pm.RemovedAt is null))
                .Select(Clone)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<ProjectMember>>(list);
        }

        public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
        {
            if (_byKey.TryGetValue((projectId, userId), out var pm))
                return Task.FromResult<ProjectRole?>(pm.Role);

            if (_roleSelector is not null)
                return Task.FromResult<ProjectRole?>(_roleSelector(projectId, userId));

            return Task.FromResult<ProjectRole?>(_fixedRole);
        }

        public Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
        {
            var count = _byKey.Values
                .Where(pm => pm.UserId == userId && pm.RemovedAt is null)
                .Select(pm => pm.ProjectId)
                .Distinct()
                .Count();

            // If no seeded data, fall back to 1 like the legacy fake, when a fixed role or selector is provided.
            if (count == 0 && (_fixedRole is not null || _roleSelector is not null))
                count = 1;

            return Task.FromResult(count);
        }

        // -------- helpers --------
        private static ProjectMember Clone(ProjectMember m)
        {
            var clone = ProjectMember.Create(m.ProjectId, m.UserId, m.Role);
            clone.RowVersion = (m.RowVersion is null) ? Array.Empty<byte>() : m.RowVersion.ToArray();
            clone.Remove(m.RemovedAt);
            return clone;
        }
    }
}
