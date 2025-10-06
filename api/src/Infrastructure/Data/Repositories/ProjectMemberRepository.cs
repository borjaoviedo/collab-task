using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class ProjectMemberRepository : IProjectMemberRepository
    {
        private readonly AppDbContext _db;
        public ProjectMemberRepository(AppDbContext db) => _db = db;

        public async Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        public async Task<IReadOnlyList<ProjectMember>> GetByProjectAsync(Guid projectId, bool includeRemoved = false, CancellationToken ct = default)
        {
            var q = _db.ProjectMembers.AsNoTracking().Where(pm => pm.ProjectId == projectId);
            if (!includeRemoved) q = q.Where(pm => pm.RemovedAt == null);
            return await q.ToListAsync(ct);
        }

        public async Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
            .AsNoTracking()
            .AnyAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        public async Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.UserId == userId && pm.ProjectId == projectId && pm.RemovedAt == null)
            .Select(pm => (ProjectRole?)pm.Role)
            .SingleOrDefaultAsync(ct);

        public async Task AddAsync(ProjectMember member, CancellationToken ct = default)
            => await _db.ProjectMembers.AddAsync(member, ct);

        public async Task<DomainMutation> UpdateRoleAsync(Guid projectId, Guid userId, ProjectRole newRole, byte[] rowVersion, CancellationToken ct = default)
        {
            var existing = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, ct);

            if (existing is null || existing.RemovedAt is not null)
                return DomainMutation.NotFound;

            if (existing.Role == newRole)
                return DomainMutation.NoOp;

            _db.Entry(existing).Property(pm => pm.RowVersion).OriginalValue = rowVersion;

            existing.ChangeRole(newRole);
            _db.Entry(existing).Property(pm => pm.Role).IsModified = true;

            return DomainMutation.Updated;
        }

        public async Task<DomainMutation> SetRemovedAsync(Guid projectId, Guid userId, DateTimeOffset? removedAt, byte[] rowVersion, CancellationToken ct = default)
        {
            var existing = await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, ct);

            if (existing is null || existing.RemovedAt is not null)
                return DomainMutation.NotFound;

            if (!removedAt.HasValue)
                return DomainMutation.NoOp;

            _db.Entry(existing).Property(pm => pm.RowVersion).OriginalValue = rowVersion;

            existing.Remove(removedAt.Value);
            _db.Entry(existing).Property(pm => pm.RemovedAt).IsModified = true;

            return DomainMutation.Updated;
        }
    }
}
