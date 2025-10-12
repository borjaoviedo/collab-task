using Application.TaskActivities.DTOs;
using Domain.Entities;

namespace Application.TaskActivities.Mapping
{
    public static class TaskActivityMapping
    {
        public static TaskActivityReadDto ToReadDto(this TaskActivity entity)
            => new()
            {
                Id = entity.Id,
                TaskId = entity.TaskId,
                ActorId = entity.ActorId,
                Type = entity.Type,
                Payload = entity.Payload.Value,
                CreatedAt = entity.CreatedAt
            };
    }
}
