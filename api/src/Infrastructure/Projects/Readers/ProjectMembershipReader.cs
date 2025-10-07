using Application.Projects.Abstractions;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Projects.Queries
{
    public sealed class ProjectMembershipReader : IProjectMembershipReader
    {
        private readonly AppDbContext _db;
        public ProjectMembershipReader(AppDbContext db) => _db = db;

        public async Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await _db.ProjectMembers
                .Where(pm => pm.ProjectId == projectId && pm.UserId == userId)
                .Select(pm => (ProjectRole?)pm.Role)
                .FirstOrDefaultAsync(ct);

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
        => await _db.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.UserId == userId && pm.RemovedAt == null)
            .Select(pm => pm.ProjectId)
            .Distinct()
            .CountAsync(ct);
    }
}
