using Application.Projects.Abstractions;
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

        public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Projects
                        .AsNoTracking()
                        .Include(p => p.Members.Where(m => m.RemovedAt == null))
                        .FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Projects
                        .Include(p => p.Members.Where(m => m.RemovedAt == null))
                        .FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task<IReadOnlyList<Project>> GetAllByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default)
        {
            filter ??= new ProjectFilter();
            var includeRemoved = filter.IncludeRemoved == true;

            IQueryable<Project> q = _db.Projects
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
            q = filter.OrderBy?.ToLowerInvariant() switch
            {
                "name" => q.OrderBy(p => p.Name).ThenBy(p => p.Id),
                "name_desc" => q.OrderByDescending(p => p.Name).ThenBy(p => p.Id),
                "createdat" => q.OrderBy(p => p.CreatedAt).ThenBy(p => p.Name),
                "createdat_desc" => q.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name),
                "updatedat" => q.OrderBy(p => p.UpdatedAt).ThenBy(p => p.Name),
                _ => q.OrderByDescending(p => p.UpdatedAt).ThenBy(p => p.Name) // default
            };

            // Paging
            var take = filter.Take is > 0 ? filter.Take.Value : 50;
            if (filter.Skip is > 0) q = q.Skip(filter.Skip.Value);
            q = q.Take(take);

            return await q.ToListAsync(ct);
        }

        public async Task AddAsync(Project project, CancellationToken ct = default)
            => await _db.Projects.AddAsync(project, ct);

        public async Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var project = await GetTrackedByIdAsync(id, ct);
            if (project is null) return DomainMutation.NotFound;
            if (project.Name == newName) return DomainMutation.NoOp;

            _db.Entry(project).Property(p => p.RowVersion).OriginalValue = rowVersion;

            project.Rename(ProjectName.Create(newName));
            _db.Entry(project).Property(p => p.Name).IsModified = true;
            _db.Entry(project).Property(p => p.Slug).IsModified = true;

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Updated;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            var project = await GetTrackedByIdAsync(id, ct);
            if (project is null) return DomainMutation.NotFound;

            _db.Entry(project).Property(p => p.RowVersion).OriginalValue = rowVersion;
            _db.Projects.Remove(project);

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Deleted;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
            catch (DbUpdateException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<bool> ExistsByNameAsync(Guid ownerId, string name, CancellationToken ct = default)
            => await _db.Projects
                        .AsNoTracking()
                        .AnyAsync(p => p.OwnerId == ownerId && p.Name == name, ct);

        public async Task<int> SaveChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);
    }
}
