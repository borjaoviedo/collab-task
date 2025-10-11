using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Services
{
    public sealed class TaskActivityWriteService(ITaskActivityRepository repo) : ITaskActivityWriteService
    {
        public async Task<(DomainMutation, TaskActivity?)> CreateAsync(
            Guid taskId, Guid actorId, TaskActivityType type, string payload, CancellationToken ct = default)
        {
            var activity = TaskActivity.Create(taskId, actorId, type, ActivityPayload.Create(payload));
            await repo.AddAsync(activity, ct);
            await repo.SaveChangesAsync(ct);
            return (DomainMutation.Created, activity);
        }

        public async Task<DomainMutation> CreateManyAsync(
            IEnumerable<(Guid TaskId, Guid ActorId, TaskActivityType Type, string Payload)> activities, CancellationToken ct = default)
        {
            var list = activities.Select(a => TaskActivity.Create(a.TaskId, a.ActorId, a.Type, ActivityPayload.Create(a.Payload))).ToList();
            if (list.Count == 0) return DomainMutation.NoOp;

            await repo.AddRangeAsync(list, ct);
            await repo.SaveChangesAsync(ct);
            return DomainMutation.Created;
        }
    }
}
