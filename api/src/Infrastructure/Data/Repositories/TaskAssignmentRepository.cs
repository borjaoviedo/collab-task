using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.Changes;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Data.Repositories
{
    public sealed class TaskAssignmentRepository(AppDbContext db) : ITaskAssignmentRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId)
                        .ToListAsync(ct);

        public async Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.UserId == userId)
                        .ToListAsync(ct);

        public async Task<TaskAssignment?> GetAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        public async Task<TaskAssignment?> GetTrackedAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        public async Task AddAsync(TaskAssignment assignment, CancellationToken ct = default)
            => await _db.TaskAssignments.AddAsync(assignment, ct);

        public async Task<(PrecheckStatus Status, AssignmentChange? Change)> ChangeRoleAsync(
            Guid taskId,
            Guid userId,
            TaskRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var existing = await GetTrackedAsync(taskId, userId, ct);
            if (existing is null) return (PrecheckStatus.NotFound, null);
            if (existing.Role == newRole) return (PrecheckStatus.NoOp, null);

            if (newRole == TaskRole.Owner)
            {
                var anotherOwner = await AnyOwnerAsync(taskId, excludeUserId: userId, ct);
                if (anotherOwner) return (PrecheckStatus.Conflict, null);
            }
            _db.Entry(existing).Property(a => a.RowVersion).OriginalValue = rowVersion;

            var change = new AssignmentRoleChangedChange(existing.Role, newRole);
            _db.Entry(existing).Property(a => a.Role).CurrentValue = newRole;
            _db.Entry(existing).Property(a => a.Role).IsModified = true;

            return (PrecheckStatus.Ready, change);
        }

        public async Task<PrecheckStatus> DeleteAsync(Guid taskId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
        {
            var existing = await GetTrackedAsync(taskId, userId, ct);
            if (existing is null) return PrecheckStatus.NotFound;

            _db.Entry(existing).Property(a => a.RowVersion).OriginalValue = rowVersion;
            _db.TaskAssignments.Remove(existing);
            return PrecheckStatus.Ready;
        }


        public async Task<bool> ExistsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .AnyAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        public async Task<bool> AnyOwnerAsync(Guid taskId, Guid? excludeUserId = null, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .AnyAsync(
                            a => a.TaskId == taskId
                                && a.Role == TaskRole.Owner
                                && (excludeUserId == null || a.UserId != excludeUserId),
                            ct);
    }
}
