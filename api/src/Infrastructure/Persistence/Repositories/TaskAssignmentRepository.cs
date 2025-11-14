using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.Changes;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="TaskAssignment"/> aggregates.
    /// Handles membership lookups, owner constraints, and concurrency-safe role changes.
    /// </summary>
    public sealed class TaskAssignmentRepository(AppDbContext db) : ITaskAssignmentRepository
    {
        private readonly AppDbContext _db = db;

        /// <summary>
        /// Lists all assignments for a specific task.
        /// </summary>
        public async Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId)
                        .ToListAsync(ct);

        /// <summary>
        /// Lists all assignments for a specific user across tasks.
        /// </summary>
        public async Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.UserId == userId)
                        .ToListAsync(ct);

        /// <summary>
        /// Gets an assignment by composite key (taskId, userId) without tracking.
        /// </summary>
        public async Task<TaskAssignment?> GetByTaskAndUserIdAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        /// <summary>
        /// Gets a tracked assignment for update operations.
        /// </summary>
        public async Task<TaskAssignment?> GetTrackedByTaskAndUserIdAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default)
            => await _db.TaskAssignments
                        .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        /// <summary>
        /// Adds a new assignment entry to the context.
        /// </summary>
        public async Task AddAsync(TaskAssignment assignment, CancellationToken ct = default)
            => await _db.TaskAssignments.AddAsync(assignment, ct);

        /// <summary>
        /// Changes a user's assignment role with concurrency and owner exclusivity enforcement.
        /// Returns both the status and the domain change descriptor when applicable.
        /// </summary>
        public async Task<(PrecheckStatus Status, AssignmentChange? Change)> ChangeRoleAsync(
            Guid taskId,
            Guid userId,
            TaskRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var existing = await GetTrackedByTaskAndUserIdAsync(taskId, userId, ct);
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

        /// <summary>
        /// Deletes a task assignment with concurrency protection.
        /// </summary>
        public async Task<PrecheckStatus> DeleteAsync(
            Guid taskId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var existing = await GetTrackedByTaskAndUserIdAsync(taskId, userId, ct);
            if (existing is null) return PrecheckStatus.NotFound;

            _db.Entry(existing).Property(a => a.RowVersion).OriginalValue = rowVersion;
            _db.TaskAssignments.Remove(existing);
            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Checks whether a given user is assigned to a task.
        /// </summary>
        public async Task<bool> ExistsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .AnyAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        /// <summary>
        /// Checks whether a task already has an owner, optionally excluding a specific user.
        /// </summary>
        public async Task<bool> AnyOwnerAsync(
            Guid taskId,
            Guid? excludeUserId = null,
            CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .AnyAsync(
                            a => a.TaskId == taskId
                                && a.Role == TaskRole.Owner
                                && (excludeUserId == null || a.UserId != excludeUserId),
                            ct);
    }
}
