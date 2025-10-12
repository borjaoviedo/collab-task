using Application.TaskActivities.DTOs;
using Domain.Entities;
using Domain.ValueObjects;

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

        public static TaskActivity ToEntity(this TaskActivityCreateDto dto)
            => TaskActivity.Create(dto.TaskId, dto.ActorId, dto.Type, ActivityPayload.Create(dto.Payload));
    }
}
