using Application.Projects.Abstractions;
using Application.Projects.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="Project"/> aggregates.
    /// Supports filtered listing by user with paging, tracked fetch,
    /// rename and delete with optimistic concurrency, and name uniqueness checks.
    /// </summary>
    public sealed class ProjectRepository(AppDbContext db) : IProjectRepository
    {
        private readonly AppDbContext _db = db;

        /// <summary>
        /// Lists projects visible to a specific user, optionally filtered by name, role, or removal status.
        /// Supports sorting, paging, and includes non-removed members by default.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="filter">Optional filter defining role, name, paging, and sorting criteria.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<Project>> ListByUserAsync(
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

        /// <summary>
        /// Gets a project by id without tracking, including non-removed members.
        /// </summary>
        public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Projects
                        .AsNoTracking()
                        .Include(p => p.Members.Where(m => m.RemovedAt == null))
                        .FirstOrDefaultAsync(p => p.Id == id, ct);

        /// <summary>
        /// Gets a tracked project including non-removed members.
        /// </summary>
        public async Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Projects
                        .Include(p => p.Members.Where(m => m.RemovedAt == null))
                        .FirstOrDefaultAsync(p => p.Id == id, ct);

        /// <summary>
        /// Adds a new project to the context.
        /// </summary>
        public async Task AddAsync(Project project, CancellationToken ct = default)
            => await _db.Projects.AddAsync(project, ct);

        /// <summary>
        /// Renames a project with concurrency protection, updating both name and slug.
        /// </summary>
        public async Task<PrecheckStatus> RenameAsync(
            Guid id,
            ProjectName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
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

        /// <summary>
        /// Deletes a project with optimistic concurrency.
        /// </summary>
        public async Task<PrecheckStatus> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            var project = await GetTrackedByIdAsync(id, ct);
            if (project is null) return PrecheckStatus.NotFound;

            _db.Entry(project).Property(p => p.RowVersion).OriginalValue = rowVersion;
            _db.Projects.Remove(project);

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Checks whether a project name already exists for a specific owner.
        /// </summary>
        public async Task<bool> ExistsByNameAsync(Guid ownerId, ProjectName name, CancellationToken ct = default)
            => await _db.Projects
                        .AsNoTracking()
                        .AnyAsync(p => p.OwnerId == ownerId && p.Name == name, ct);
    }
}
