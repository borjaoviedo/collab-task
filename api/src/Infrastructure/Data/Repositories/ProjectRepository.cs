using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _db;
        public ProjectRepository(AppDbContext db) => _db = db;
        public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task<IReadOnlyList<Project>> GetByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default)
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

        public Task UpdateAsync(Project project, CancellationToken ct = default)
        {
            if (_db.Entry(project).State == EntityState.Detached)
                _db.Projects.Attach(project);

            _db.Entry(project).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Project project, CancellationToken ct = default)
        {
            if (_db.Entry(project).State == EntityState.Detached)
                _db.Projects.Attach(project);

            _db.Projects.Remove(project);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsByNameAsync(Guid ownerId, ProjectName name, CancellationToken ct = default)
            => await _db.Projects
                .AsNoTracking()
                .AnyAsync(p => p.OwnerId == ownerId && p.Name == name, ct);
    }
}
