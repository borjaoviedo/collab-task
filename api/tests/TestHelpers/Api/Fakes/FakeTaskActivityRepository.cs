using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using TestHelpers.Common.Time;

namespace TestHelpers.Api.Fakes
{
    public sealed class FakeTaskActivityRepository : ITaskActivityRepository
    {
        private readonly Dictionary<Guid, TaskActivity> _store = [];

        public Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(activityId, out var a) ? Clone(a) : null);

        public Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskActivity>>(_store.Values.Where(a => a.TaskId == taskId)
                .OrderBy(a => a.CreatedAt).Select(Clone).ToList());

        public Task<IReadOnlyList<TaskActivity>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskActivity>>(_store.Values.Where(a => a.ActorId == userId)
                .OrderBy(a => a.CreatedAt).Select(Clone).ToList());

        public Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(Guid taskId, TaskActivityType type, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskActivity>>(_store.Values.Where(a => a.TaskId == taskId && a.Type == type)
                .OrderBy(a => a.CreatedAt).Select(Clone).ToList());

        public Task AddAsync(TaskActivity activity, CancellationToken ct = default)
        {
            _store[activity.Id] = activity;
            return Task.CompletedTask;
        }

        private static TaskActivity Clone(TaskActivity a)
            => TaskActivity.Create(
                            a.TaskId,
                            a.ActorId,
                            a.Type,
                            ActivityPayload.Create(a.Payload),
                            createdAt: TestTime.FixedNow);
    }
}
