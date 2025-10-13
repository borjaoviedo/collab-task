using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Abstractions
{
    public interface ITaskActivityWriteService
    {
        Task<(DomainMutation, TaskActivity?)> CreateAsync(
            Guid taskId, Guid actorId, TaskActivityType type, string payload, CancellationToken ct = default);

        Task<DomainMutation> CreateManyAsync(
            IEnumerable<(Guid TaskId, Guid ActorId, TaskActivityType Type, string Payload)> activities, CancellationToken ct = default);
    }
}
