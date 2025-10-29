using Application.ProjectMembers.Abstractions;
using Application.Projects.Abstractions;
using Application.Projects.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using System.Collections.Concurrent;

namespace TestHelpers.Api.Fakes
{
    public sealed class FakeProjectRepository(IProjectMemberRepository pmRepo) : IProjectRepository
    {
        // ProjectId -> tracked Project
        private readonly ConcurrentDictionary<Guid, Project> _byId = new();

        // (OwnerId, Name) unique index
        private readonly ConcurrentDictionary<(Guid OwnerId, string Name), byte> _nameIndex = new();

        // simple rowversion counter
        private long _rv = 1;

        private readonly IProjectMemberRepository _pmRepo = pmRepo;

        public Task<IReadOnlyList<Project>> ListByUserAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default)
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
                q = q
                    .Where(p => p.Members
                    .Any(m => m.UserId == userId && m.Role == role && (includeRemoved || m.RemovedAt is null)));
            }

            q = filter.OrderBy switch
            {
                ProjectOrderBy.NameAsc =>
                    q.OrderBy(p => p.Name.Value).ThenBy(p => p.Id),

                ProjectOrderBy.NameDesc =>
                    q.OrderByDescending(p => p.Name.Value).ThenBy(p => p.Id),

                ProjectOrderBy.CreatedAtAsc =>
                    q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Name.Value),

                ProjectOrderBy.CreatedAtDesc =>
                    q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name.Value),

                ProjectOrderBy.UpdatedAtAsc =>
                    q.OrderBy(p => p.UpdatedAt).ThenBy(p => p.Name.Value),

                ProjectOrderBy.UpdatedAtDesc or _ =>
                    q.OrderByDescending(p => p.UpdatedAt).ThenBy(p => p.Name.Value)
            };

            var take = filter.Take is > 0 ? filter.Take.Value : 50;
            if (filter.Skip is > 0) q = q.Skip(filter.Skip.Value);
            q = q.Take(take);

            var list = q.ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<Project>>(list);
        }

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (_byId.TryGetValue(id, out var p))
                return Task.FromResult<Project?>(p);

            return Task.FromResult<Project?>(null);
        }

        public Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
        {
            _byId.TryGetValue(id, out var p);
            // return tracked instance to simulate EF tracking
            return Task.FromResult(p);
        }

        public Task AddAsync(Project project, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(project);

            // ensure rowversion like EF side-effect
            if (project.RowVersion is null || project.RowVersion.Length == 0)
                project.SetRowVersion(NextRowVersion());

            if (!_byId.TryAdd(project.Id, project))
                throw new InvalidOperationException("Duplicate project id.");

            if (!_nameIndex.TryAdd((project.OwnerId, project.Name.Value), 0))
                throw new InvalidOperationException("Duplicate project name for owner.");

            foreach (var m in project.Members)
            {
                var cm = ProjectMember.Create(m.ProjectId, m.UserId, m.Role);
                var rowVersion = (m.RowVersion is null) ? [] : m.RowVersion.ToArray();
                cm.SetRowVersion(rowVersion);
                cm.Remove(m.RemovedAt);
                _pmRepo.AddAsync(cm, ct);
            }

            return Task.CompletedTask;
        }

        public Task<PrecheckStatus> RenameAsync(
            Guid id,
            ProjectName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(PrecheckStatus.Conflict);

            if (!_byId.TryGetValue(id, out var current))
                return Task.FromResult(PrecheckStatus.NotFound);

            if (current.Name == ProjectName.Create(newName))
                return Task.FromResult(PrecheckStatus.NoOp);

            if (!RowVersionEquals(current.RowVersion, rowVersion))
                return Task.FromResult(PrecheckStatus.Conflict);

            // uniqueness per owner
            if (_nameIndex.ContainsKey((current.OwnerId, newName)))
                return Task.FromResult(PrecheckStatus.Conflict);

            // mutate tracked instance to simulate EF
            current.Rename(ProjectName.Create(newName));
            current.SetRowVersion(NextRowVersion());

            // update name index
            _nameIndex.TryRemove((current.OwnerId, current.Name.Value), out _);
            _nameIndex.TryAdd((current.OwnerId, newName), 0);

            return Task.FromResult(PrecheckStatus.Ready);
        }

        public Task<PrecheckStatus> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                return Task.FromResult(PrecheckStatus.Conflict);

            if (!_byId.TryGetValue(id, out var current))
                return Task.FromResult(PrecheckStatus.NotFound);

            if (!RowVersionEquals(current.RowVersion, rowVersion))
                return Task.FromResult(PrecheckStatus.Conflict);

            _byId.TryRemove(id, out _);
            _nameIndex.TryRemove((current.OwnerId, current.Name.Value), out _);
            return Task.FromResult(PrecheckStatus.Ready);
        }

        public Task<bool> ExistsByNameAsync(Guid ownerId, ProjectName name, CancellationToken ct = default)
            => Task.FromResult(_nameIndex.ContainsKey((ownerId, name)));

        // ----------------- helpers -----------------

        private static bool RowVersionEquals(byte[] a, byte[] b)
            => a is not null && b is not null && a.Length == b.Length && a.SequenceEqual(b);

        private byte[] NextRowVersion()
            => BitConverter.GetBytes(Interlocked.Increment(ref _rv));
    }
}
