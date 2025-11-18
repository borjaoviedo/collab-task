using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="ProjectMember"/> aggregates.
    /// Provides optimized read and write operations for project memberships,
    /// including lookups by project/user, role resolution, existence checks,
    /// and membership analytics. Read operations use <c>AsNoTracking()</c>
    /// where appropriate to maximize query performance, while update operations
    /// rely on EF Core change tracking to generate minimal UPDATE statements.
    /// </summary>
    /// <param name="db">
    /// The <see cref="CollabTaskDbContext"/> used to query and persist
    /// <see cref="ProjectMember"/> entities.
    /// </param>
    public sealed class ProjectMemberRepository(CollabTaskDbContext db) : IProjectMemberRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProjectMember>> ListByProjectIdAsync(
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

        /// <inheritdoc/>
        public async Task<ProjectMember?> GetByProjectAndUserIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Include(pm => pm.User)
                        .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        /// <inheritdoc/>
        public async Task<ProjectMember?> GetByProjectAndUserIdForUpdateAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default)
            => await _db.ProjectMembers.FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        /// <inheritdoc/>
        public async Task<ProjectRole?> GetUserRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
                        .Select(pm => (ProjectRole?)pm.Role)
                        .FirstOrDefaultAsync(ct);

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .AnyAsync(pm => pm.UserId == userId && pm.ProjectId == projectId, ct);

        /// <inheritdoc/>
        public async Task<int> CountUserActiveMembershipsAsync(Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                        .AsNoTracking()
                        .Where(pm => pm.UserId == userId && pm.RemovedAt == null)
                        .Select(pm => pm.ProjectId)
                        .Distinct()
                        .CountAsync(ct);

        /// <inheritdoc/>
        public async Task AddAsync(ProjectMember member, CancellationToken ct = default)
            => await _db.ProjectMembers.AddAsync(member, ct);

        /// <inheritdoc/>
        public async Task UpdateAsync(ProjectMember member, CancellationToken ct = default)
        {
            // If entity is already tracked, do nothing so EF change tracking produces minimal UPDATEs
            if (_db.Entry(member).State == EntityState.Detached)
                _db.ProjectMembers.Update(member);

            await Task.CompletedTask;
        }
    }
}
