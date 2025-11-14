using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="TaskActivity"/> entries.
    /// Provides chronological listings and insertion of new activity records.
    /// </summary>
    public sealed class TaskActivityRepository(AppDbContext db) : ITaskActivityRepository
    {
        private readonly AppDbContext _db = db;

        /// <summary>
        /// Lists all activity entries for a specific task ordered by creation time.
        /// </summary>
        public async Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        /// <summary>
        /// Lists all activities performed by a specific user.
        /// </summary>
        public async Task<IReadOnlyList<TaskActivity>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.ActorId == userId)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        /// <summary>
        /// Lists activities of a specific type under a task.
        /// </summary>
        public async Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(
            Guid taskId,
            TaskActivityType type,
            CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId && a.Type == type)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        /// <summary>
        /// Gets an activity entry by its id.
        /// </summary>
        public async Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == activityId, ct);

        /// <summary>
        /// Adds a new activity entry to the context.
        /// </summary>
        public async Task AddAsync(TaskActivity activity, CancellationToken ct = default)
            => await _db.TaskActivities.AddAsync(activity, ct);
    }

}
