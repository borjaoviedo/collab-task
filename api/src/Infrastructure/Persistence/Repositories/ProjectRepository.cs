using Application.Projects.Abstractions;
using Application.Projects.Filters;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="Project"/> aggregates.
    /// Supports filtered listing by user with paging, tracked fetch,
    /// rename and delete with optimistic concurrency, and name uniqueness checks.
    /// </summary>
    public sealed class ProjectRepository(CollabTaskDbContext db) : IProjectRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Project>> ListByUserIdAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default)
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

            // Ordering and paging
            q = filter.OrderBy switch
            {
                ProjectOrderBy.NameAsc => q.OrderBy(p => p.Name).ThenBy(p => p.Id),
                ProjectOrderBy.NameDesc => q.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
                ProjectOrderBy.CreatedAtAsc => q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Name),
                ProjectOrderBy.CreatedAtDesc => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name),
                ProjectOrderBy.UpdatedAtAsc => q.OrderBy(p => p.UpdatedAt).ThenBy(p => p.Name),
                ProjectOrderBy.UpdatedAtDesc or _ => q.OrderByDescending(p => p.UpdatedAt).ThenBy(p => p.Name)
            };

            var take = filter.Take is > 0 ? filter.Take.Value : 50;
            if (filter.Skip is > 0) q = q.Skip(filter.Skip.Value);
            q = q.Take(take);

            return await q.ToListAsync(ct);
        }

        /// <inheritdoc/>
        public async Task<Project?> GetByIdAsync(Guid projectId, CancellationToken ct = default)
            => await _db.Projects
                        .AsNoTracking()
                        .Include(p => p.Members.Where(m => m.RemovedAt == null))
                        .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        /// <inheritdoc/>
        public async Task<Project?> GetByIdForUpdateAsync(Guid projectId, CancellationToken ct = default)
            => await _db.Projects
                        .Include(p => p.Members.Where(m => m.RemovedAt == null))
                        .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        /// <inheritdoc/>
        public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
            => await _db.Projects
                        .AsNoTracking()
                        .AnyAsync(p => p.Name == name, ct);

        /// <inheritdoc/>
        public async Task AddAsync(Project project, CancellationToken ct = default)
            => await _db.Projects.AddAsync(project, ct);

        /// <inheritdoc/>
        public async Task UpdateAsync(Project project, CancellationToken ct = default)
        {
            // If entity is already tracked, do nothing so EF change tracking produces minimal UPDATEs
            if (_db.Entry(project).State == EntityState.Detached)
                _db.Projects.Update(project);

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async  Task RemoveAsync(Project project, CancellationToken ct = default)
        {
            // Mark entity as deleted; actual deletion occurs in UnitOfWork.SaveAsync()
            _db.Projects.Remove(project);
            await Task.CompletedTask;
        }
    }
}
