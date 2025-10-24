using Application.Common.Abstractions.Time;
using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Services
{
    public sealed class TaskActivityWriteService(ITaskActivityRepository repo, IDateTimeProvider clock) : ITaskActivityWriteService
    {
        public async Task<(DomainMutation, TaskActivity?)> CreateAsync(
            Guid taskId,
            Guid userId,
            TaskActivityType type,
            ActivityPayload payload,
            CancellationToken ct = default)
        {
            var activity = TaskActivity.Create(taskId, userId, type, payload, clock.UtcNow);

            await repo.AddAsync(activity, ct);
            return (DomainMutation.Created, activity);
        }
    }
}
