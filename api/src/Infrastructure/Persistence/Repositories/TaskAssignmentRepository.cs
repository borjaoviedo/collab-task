using Application.TaskAssignments.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="TaskAssignment"/> aggregates.
    /// Provides optimized read and write access to task–user assignment relationships,
    /// including listing by task or user, ownership checks, tracked and untracked
    /// retrieval workflows, and CRUD operations. Assignment records are central to
    /// enforcing collaboration rules such as “exactly one owner per task,” and this
    /// repository exposes the querying primitives required by the application layer
    /// to maintain those invariants. Read operations use <c>AsNoTracking()</c> for
    /// efficiency, while update operations rely on EF Core change tracking to
    /// generate minimal persistence operations.
    /// </summary>
    /// <param name="db">
    /// The <see cref="CollabTaskDbContext"/> used to query and persist
    /// <see cref="TaskAssignment"/> entities.
    /// </param>
    public sealed class TaskAssignmentRepository(CollabTaskDbContext db) : ITaskAssignmentRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskAssignment>> ListByTaskIdAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskAssignment>> ListByUserIdAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .Where(a => a.UserId == userId)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<TaskAssignment?> GetByTaskAndUserIdAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        /// <inheritdoc/>
        public async Task<TaskAssignment?> GetByTaskAndUserIdForUpdateAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default)
            => await _db.TaskAssignments
                        .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await _db.TaskAssignments
                        .AsNoTracking()
                        .AnyAsync(a => a.TaskId == taskId && a.UserId == userId, ct);

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task AddAsync(TaskAssignment assignment, CancellationToken ct = default)
            => await _db.TaskAssignments.AddAsync(assignment, ct);

        /// <inheritdoc/>
        public async Task UpdateAsync(TaskAssignment assignment, CancellationToken ct = default)
        {
            // If entity is already tracked, do nothing so EF change tracking produces minimal UPDATEs
            if (_db.Entry(assignment).State == EntityState.Detached)
                _db.TaskAssignments.Update(assignment);

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(TaskAssignment assignment, CancellationToken ct = default)
        {
            // Mark entity as deleted; actual deletion occurs in UnitOfWork.SaveAsync()
            _db.TaskAssignments.Remove(assignment);
            await Task.CompletedTask;
        }
    }
}
