using Application.Projects.Abstractions;
using Application.Projects.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Data.Repositories
{
    public sealed class ProjectRepository(AppDbContext db) : IProjectRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<IReadOnlyList<Project>> GetAllByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default)
        {
            filter ??= new ProjectFilter();
            var includeRemoved = filter.IncludeRemoved == true;

            var q = _db.Projects
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Include(p => p.Members.Where(m => includeRemoved || m.RemovedAt == null))
                        .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId && (includeRemoved || m.RemovedAt == null)));

            if (!string.IsNullOrWhiteSpace(filter.NameContains))
            {
                var term = filter.NameContains.Trim();
                q = q.Where(p => EF.Functions.Like(p.Name, $"%{term}%"));
            }

            if (filter.Role is not null)
            {
                var role = filter.Role.Value;
                q = q.Where(p => p.Members.Any(m => m.UserId == userId && m.Role == role && (includeRemoved || m.RemovedAt == null)));
            }

            // Ordering
            q = filter.OrderBy switch
            {
                ProjectOrderBy.NameAsc =>
                    q.OrderBy(p => p.Name).ThenBy(p => p.Id),

                ProjectOrderBy.NameDesc =>
                    q.OrderByDescending(p => p.Name).ThenBy(p => p.Id),

                ProjectOrderBy.CreatedAtAsc =>
                    q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Name),

                ProjectOrderBy.CreatedAtDesc =>
                    q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name),

                ProjectOrderBy.UpdatedAtAsc =>
                    q.OrderBy(p => p.UpdatedAt).ThenBy(p => p.Name),

                ProjectOrderBy.UpdatedAtDesc or _ =>
                    q.OrderByDescending(p => p.UpdatedAt).ThenBy(p => p.Name)
            };

            // Paging
            var take = filter.Take is > 0 ? filter.Take.Value : 50;
            if (filter.Skip is > 0) q = q.Skip(filter.Skip.Value);
            q = q.Take(take);

            return await q.ToListAsync(ct);
        }

        public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Projects
                        .AsNoTracking()
                        .Include(p => p.Members.Where(m => m.RemovedAt == null))
                        .FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Projects
                        .Include(p => p.Members.Where(m => m.RemovedAt == null))
                        .FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task AddAsync(Project project, CancellationToken ct = default)
            => await _db.Projects.AddAsync(project, ct);

        public async Task<PrecheckStatus> RenameAsync(Guid id, ProjectName newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var project = await GetTrackedByIdAsync(id, ct);
            if (project is null) return PrecheckStatus.NotFound;
            if (project.Name == newName) return PrecheckStatus.NoOp;

            _db.Entry(project).Property(p => p.RowVersion).OriginalValue = rowVersion;

            project.Rename(newName);
            _db.Entry(project).Property(p => p.Name).IsModified = true;
            _db.Entry(project).Property(p => p.Slug).IsModified = true;

            return PrecheckStatus.Ready;
        }

        public async Task<PrecheckStatus> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            var project = await GetTrackedByIdAsync(id, ct);
            if (project is null) return PrecheckStatus.NotFound;

            _db.Entry(project).Property(p => p.RowVersion).OriginalValue = rowVersion;
            _db.Projects.Remove(project);

            return PrecheckStatus.Ready;
        }

        public async Task<bool> ExistsByNameAsync(Guid ownerId, ProjectName name, CancellationToken ct = default)
            => await _db.Projects
                        .AsNoTracking()
                        .AnyAsync(p => p.OwnerId == ownerId && p.Name == name, ct);
    }
}
