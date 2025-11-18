using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="TaskActivity"/> aggregates.
    /// Provides optimized read operations for retrieving task activity logs,
    /// including filtered lists by task, user, or activity type, and single-entry lookup.
    /// All read queries use <c>AsNoTracking()</c> for maximum performance since
    /// activity records are immutable audit entries. Write operations rely on the
    /// application layer to persist new activity entries that describe changes made
    /// to tasks.
    /// </summary>
    /// <param name="db">
    /// The <see cref="CollabTaskDbContext"/> used to query and persist
    /// <see cref="TaskActivity"/> entities.
    /// </param>
    public sealed class TaskActivityRepository(CollabTaskDbContext db) : ITaskActivityRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskActivity>> ListByTaskIdAsync(
            Guid taskId,
            CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskActivity>> ListByUserIdAsync(
            Guid userId,
            CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.ActorId == userId)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskActivity>> ListByTaskTypeAsync(
            Guid taskId,
            TaskActivityType type,
            CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId && a.Type == type)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == activityId, ct);

        /// <inheritdoc/>
        public async Task AddAsync(TaskActivity activity, CancellationToken ct = default)
            => await _db.TaskActivities.AddAsync(activity, ct);
    }
}
