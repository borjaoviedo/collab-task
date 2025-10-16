using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Abstractions
{
    public interface ITaskActivityWriteService
    {
        Task<(DomainMutation, TaskActivity?)> CreateAsync(
            Guid taskId,
            Guid actorId,
            TaskActivityType type,
            ActivityPayload payload,
            CancellationToken ct = default);
    }
}
