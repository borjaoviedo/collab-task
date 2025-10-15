using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Services
{
    public sealed class TaskActivityWriteService(ITaskActivityRepository repo) : ITaskActivityWriteService
    {
        public async Task<(DomainMutation, TaskActivity?)> CreateAsync(
            Guid taskId, Guid actorId, TaskActivityType type, ActivityPayload payload, CancellationToken ct = default)
        {
            var activity = TaskActivity.Create(taskId, actorId, type, payload);
            activity.CreatedAt = DateTimeOffset.UtcNow;

            await repo.AddAsync(activity, ct);
            return (DomainMutation.Created, activity);
        }
    }
}
