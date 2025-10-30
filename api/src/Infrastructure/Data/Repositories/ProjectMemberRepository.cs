using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="ProjectMember"/>.
    /// Supports membership queries, role lookup, tracked updates with optimistic concurrency,
    /// and soft remove/restore semantics respecting <see cref="ProjectMember.RemovedAt"/>.
    /// </summary>
    public sealed class ProjectMemberRepository(AppDbContext db) : IProjectMemberRepository
    {
        private readonly AppDbContext _db = db;

        /// <summary>
        /// Lists memberships for a project. Optionally includes removed entries.
        /// Includes the related <see cref="User"/> for convenience.
        /// </summary>
        public async Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(
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

        /// <summary>
        /// Gets a membership by project and user without tracking, including <see cref="User"/>.
        /// </summary>
        public async Task<ProjectMember?> GetByProjectAndUserIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Include(pm => pm.User)
                        .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        /// <summary>
        /// Gets a tracked membership by project and user.
        /// </summary>
        public async Task<ProjectMember?> GetTrackedByProjectAndUserIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
            => await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        /// <summary>
        /// Gets the role of a user within a project, or <c>null</c> if not a member.
        /// </summary>
        public async Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
                        .Select(pm => (ProjectRole?)pm.Role)
                        .FirstOrDefaultAsync(ct);

        /// <summary>
        /// Adds a new project membership to the context.
        /// </summary>
        public async Task AddAsync(ProjectMember member, CancellationToken ct = default)
            => await _db.ProjectMembers.AddAsync(member, ct);

        /// <summary>
        /// Updates a member's role with optimistic concurrency. No-ops if the role is unchanged.
        /// </summary>
        public async Task<PrecheckStatus> UpdateRoleAsync(
            Guid projectId,
            Guid userId,
            ProjectRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var projectMember = await GetTrackedByProjectAndUserIdAsync(projectId, userId, ct);

            if (projectMember is null || projectMember.RemovedAt is not null)
                return PrecheckStatus.NotFound;

            if (projectMember.Role == newRole)
                return PrecheckStatus.NoOp;

            _db.Entry(projectMember).Property(pm => pm.RowVersion).OriginalValue = rowVersion;

            projectMember.ChangeRole(newRole);
            _db.Entry(projectMember).Property(pm => pm.Role).IsModified = true;

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Marks a membership as removed (soft delete) with concurrency protection.
        /// </summary>
        public async Task<PrecheckStatus> SetRemovedAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var projectMember = await GetTrackedByProjectAndUserIdAsync(projectId, userId, ct);
            if (projectMember is null) return PrecheckStatus.NotFound;

            var now = DateTimeOffset.UtcNow;
            if (projectMember.RemovedAt == now) return PrecheckStatus.NoOp;

            _db.Entry(projectMember).Property(pm => pm.RowVersion).OriginalValue = rowVersion;

            projectMember.Remove(now);
            _db.Entry(projectMember).Property(pm => pm.RemovedAt).IsModified = true;

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Restores a previously removed membership with concurrency protection.
        /// </summary>
        public async Task<PrecheckStatus> SetRestoredAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var projectMember = await GetTrackedByProjectAndUserIdAsync(projectId, userId, ct);
            if (projectMember is null) return PrecheckStatus.NotFound;

            if (projectMember.RemovedAt == null) return PrecheckStatus.NoOp;

            _db.Entry(projectMember).Property(pm => pm.RowVersion).OriginalValue = rowVersion;

            projectMember.Restore();
            _db.Entry(projectMember).Property(pm => pm.RemovedAt).IsModified = true;

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Checks whether a user has a membership entry in a project.
        /// </summary>
        public async Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .AnyAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        /// <summary>
        /// Counts distinct projects where the user has an active membership.
        /// </summary>
        public async Task<int> CountUserActiveMembershipsAsync(Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Where(pm => pm.UserId == userId && pm.RemovedAt == null)
                        .Select(pm => pm.ProjectId)
                        .Distinct()
                        .CountAsync(ct);
    }

}
