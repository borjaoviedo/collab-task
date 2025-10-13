using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Api.Tests.Fakes
{
    public sealed class FakeTaskActivityRepository : ITaskActivityRepository
    {
        private readonly Dictionary<Guid, TaskActivity> _store = [];

        public Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(activityId, out var a) ? Clone(a) : null);

        public Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskActivity>>(_store.Values.Where(a => a.TaskId == taskId)
                .OrderBy(a => a.CreatedAt).Select(Clone).ToList());

        public Task<IReadOnlyList<TaskActivity>> ListByActorAsync(Guid actorId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskActivity>>(_store.Values.Where(a => a.ActorId == actorId)
                .OrderBy(a => a.CreatedAt).Select(Clone).ToList());

        public Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(Guid taskId, TaskActivityType type, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskActivity>>(_store.Values.Where(a => a.TaskId == taskId && a.Type == type)
                .OrderBy(a => a.CreatedAt).Select(Clone).ToList());

        public Task AddAsync(TaskActivity activity, CancellationToken ct = default)
        {
            _store[activity.Id] = activity;
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<TaskActivity> activities, CancellationToken ct = default)
        {
            foreach (var a in activities) _store[a.Id] = a;
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);

        private static TaskActivity Clone(TaskActivity a)
            => TaskActivity.Create(a.TaskId, a.ActorId, a.Type, ActivityPayload.Create(a.Payload));
    }
}
