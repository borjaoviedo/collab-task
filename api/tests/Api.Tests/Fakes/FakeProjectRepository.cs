using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using System.Collections.Concurrent;

namespace Api.Tests.Fakes
{
    public sealed class FakeProjectRepository : IProjectRepository
    {
        // ProjectId -> Project
        private readonly ConcurrentDictionary<Guid, Project> _byId = new();
        // (OwnerId, ProjectName.Value) unique index
        private readonly ConcurrentDictionary<(Guid OwnerId, string Name), byte> _nameIndex = new();

        // simple rowversion counter
        private long _rv = 1;

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_byId.TryGetValue(id, out var p))
                return Task.FromResult<Project?>(CloneProject(p, includeRemovedMembers: false));

            return Task.FromResult<Project?>(null);
        }

        public Task<IReadOnlyList<Project>> GetByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default)
        {
            filter ??= new ProjectFilter();
            var includeRemoved = filter.IncludeRemoved == true;

            IEnumerable<Project> q = _byId.Values.Where(p =>
                p.OwnerId == userId ||
                p.Members.Any(m => m.UserId == userId && (includeRemoved || m.RemovedAt is null))
            );

            if (!string.IsNullOrWhiteSpace(filter.NameContains))
            {
                var term = filter.NameContains.Trim();
                q = q.Where(p => p.Name.Value.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.Role is not null)
            {
                var role = filter.Role.Value;
                q = q.Where(p => p.Members.Any(m => m.UserId == userId && m.Role == role && (includeRemoved || m.RemovedAt is null)));
            }

            q = (filter.OrderBy?.ToLowerInvariant()) switch
            {
                "name" => q.OrderBy(p => p.Name.Value).ThenBy(p => p.Id),
                "name_desc" => q.OrderByDescending(p => p.Name.Value).ThenBy(p => p.Id),
                "createdat" => q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Name.Value),
                "createdat_desc" => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name.Value),
                "updatedat" => q.OrderBy(p => p.UpdatedAt).ThenBy(p => p.Name.Value),
                _ => q.OrderByDescending(p => p.UpdatedAt).ThenBy(p => p.Name.Value)
            };

            var take = filter.Take is > 0 ? filter.Take.Value : 50;
            if (filter.Skip is > 0) q = q.Skip(filter.Skip.Value);
            q = q.Take(take);

            var list = q.Select(p => CloneProject(p, includeRemoved)).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<Project>>(list);
        }

        public Task AddAsync(Project project, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(project);

            // ensure rowversion and slug
            if (project.RowVersion is null || project.RowVersion.Length == 0)
                project.RowVersion = NextRowVersion();

            if (string.IsNullOrWhiteSpace(project.Slug))
                project.Slug = ProjectSlug.Create(project.Name.Value);

            if (!_byId.TryAdd(project.Id, CloneProject(project, includeRemovedMembers: true)))
                throw new InvalidOperationException("Duplicate project id.");

            if (!_nameIndex.TryAdd((project.OwnerId, project.Name.Value), 0))
                throw new InvalidOperationException("Duplicate project name for owner.");

            return Task.CompletedTask;
        }

        public Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(DomainMutation.Conflict);

            if (!_byId.TryGetValue(id, out var current))
                return Task.FromResult(DomainMutation.NotFound);

            if (current.Name == ProjectName.Create(newName))
                return Task.FromResult(DomainMutation.NoOp);

            if (!RowVersionEquals(current.RowVersion, rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            // uniqueness check per owner
            if (_nameIndex.ContainsKey((current.OwnerId, newName)))
                return Task.FromResult(DomainMutation.Conflict);

            var updated = CloneProject(current, includeRemovedMembers: true);
            updated.Rename(ProjectName.Create(newName));
            updated.Slug = ProjectSlug.Create(newName);
            updated.RowVersion = NextRowVersion();

            _byId[id] = updated;

            // update name index
            _nameIndex.TryRemove((current.OwnerId, current.Name.Value), out _);
            _nameIndex.TryAdd((updated.OwnerId, updated.Name.Value), 0);

            return Task.FromResult(DomainMutation.Updated);
        }

        public Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(DomainMutation.Conflict);

            if (!_byId.TryGetValue(id, out var current))
                return Task.FromResult(DomainMutation.NotFound);

            if (!RowVersionEquals(current.RowVersion, rowVersion))
                return Task.FromResult(DomainMutation.Conflict);

            _byId.TryRemove(id, out _);
            _nameIndex.TryRemove((current.OwnerId, current.Name.Value), out _);
            return Task.FromResult(DomainMutation.Deleted);
        }

        public Task<bool> ExistsByNameAsync(Guid ownerId, ProjectName name, CancellationToken ct = default)
            => Task.FromResult(_nameIndex.ContainsKey((ownerId, name.Value)));

        // ----------------- helpers -----------------


        private static bool RowVersionEquals(byte[] a, byte[] b)
            => a.Length == b.Length && a.SequenceEqual(b);

        private byte[] NextRowVersion()
            => BitConverter.GetBytes(Interlocked.Increment(ref _rv));

        private static Project CloneProject(Project p, bool includeRemovedMembers)
        {
            // shallow clone of core fields
            var clone = Project.Create(p.OwnerId, p.Name, DateTimeOffset.UtcNow);
            clone.Id = p.Id;
            clone.CreatedAt = p.CreatedAt;
            clone.UpdatedAt = p.UpdatedAt;
            clone.Slug = p.Slug;
            clone.RowVersion = (p.RowVersion is null) ? Array.Empty<byte>() : p.RowVersion.ToArray();

            // members
            foreach (var m in p.Members)
            {
                if (!includeRemovedMembers && m.RemovedAt is not null) continue;
                var cm = ProjectMember.Create(m.ProjectId, m.UserId, m.Role, m.JoinedAt);
                cm.RowVersion = (m.RowVersion is null) ? Array.Empty<byte>() : m.RowVersion.ToArray();
                clone.Members.Add(cm);
            }

            return clone;
        }
    }
}
