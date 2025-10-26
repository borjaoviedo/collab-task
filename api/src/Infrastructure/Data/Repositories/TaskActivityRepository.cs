using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class TaskActivityRepository(AppDbContext db) : ITaskActivityRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        public async Task<IReadOnlyList<TaskActivity>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.ActorId == userId)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        public async Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(Guid taskId, TaskActivityType type, CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .Where(a => a.TaskId == taskId && a.Type == type)
                        .OrderBy(a => a.CreatedAt)
                        .ToListAsync(ct);

        public async Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default)
            => await _db.TaskActivities
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == activityId, ct);

        public async Task AddAsync(TaskActivity activity, CancellationToken ct = default)
            => await _db.TaskActivities.AddAsync(activity, ct);

        public async Task AddRangeAsync(IEnumerable<TaskActivity> activities, CancellationToken ct = default)
            => await _db.TaskActivities.AddRangeAsync(activities, ct);
    }
}
