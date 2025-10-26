using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class ProjectMemberRepository(AppDbContext db) : IProjectMemberRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<IReadOnlyList<ProjectMember>> GetByProjectAsync(
            Guid projectId,
            bool includeRemoved = false,
            CancellationToken ct = default)
        {
            var q = _db.ProjectMembers
                        .AsNoTracking()
                        .Where(pm => pm.ProjectId == projectId);

            if (!includeRemoved)
                q = q.Where(pm => pm.RemovedAt == null);

            return await q.Include(pm => pm.User).ToListAsync(ct);
        }

        public async Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Include(pm => pm.User)
                        .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        public async Task<ProjectMember?> GetTrackedByIdAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        public async Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
                        .Select(pm => (ProjectRole?)pm.Role)
                        .FirstOrDefaultAsync(ct);

        public async Task AddAsync(ProjectMember member, CancellationToken ct = default)
            => await _db.ProjectMembers.AddAsync(member, ct);

        public async Task<PrecheckStatus> UpdateRoleAsync(
            Guid projectId,
            Guid userId,
            ProjectRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var projectMember = await GetTrackedByIdAsync(projectId, userId, ct);

            if (projectMember is null || projectMember.RemovedAt is not null)
                return PrecheckStatus.NotFound;

            if (projectMember.Role == newRole)
                return PrecheckStatus.NoOp;

            _db.Entry(projectMember).Property(pm => pm.RowVersion).OriginalValue = rowVersion;

            projectMember.ChangeRole(newRole);
            _db.Entry(projectMember).Property(pm => pm.Role).IsModified = true;

            return PrecheckStatus.Ready;
        }

        public async Task<PrecheckStatus> SetRemovedAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var projectMember = await GetTrackedByIdAsync(projectId, userId, ct);
            if (projectMember is null) return PrecheckStatus.NotFound;

            var now = DateTimeOffset.UtcNow;
            if (projectMember.RemovedAt == now) return PrecheckStatus.NoOp;

            _db.Entry(projectMember).Property(pm => pm.RowVersion).OriginalValue = rowVersion;

            projectMember.Remove(now);
            _db.Entry(projectMember).Property(pm => pm.RemovedAt).IsModified = true;

            return PrecheckStatus.Ready;
        }

        public async Task<PrecheckStatus> SetRestoredAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var projectMember = await GetTrackedByIdAsync(projectId, userId, ct);
            if (projectMember is null) return PrecheckStatus.NotFound;

            if (projectMember.RemovedAt == null) return PrecheckStatus.NoOp;

            _db.Entry(projectMember).Property(pm => pm.RowVersion).OriginalValue = rowVersion;

            projectMember.Restore();
            _db.Entry(projectMember).Property(pm => pm.RemovedAt).IsModified = true;

            return PrecheckStatus.Ready;
        }

        public async Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .AnyAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        public async Task<int> CountUserActiveMembershipsAsync(Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Where(pm => pm.UserId == userId && pm.RemovedAt == null)
                        .Select(pm => pm.ProjectId)
                        .Distinct()
                        .CountAsync(ct);
    }
}
