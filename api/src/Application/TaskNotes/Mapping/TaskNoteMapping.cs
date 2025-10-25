using Application.TaskNotes.DTOs;
using Domain.Entities;

namespace Application.TaskNotes.Mapping
{
    public static class TaskNoteMapping
    {
        public static TaskNoteReadDto ToReadDto(this TaskNote entity)
            => new()
            {
                Id = entity.Id,
                TaskId = entity.TaskId,
                UserId = entity.UserId,
                Content = entity.Content.Value,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                RowVersion = entity.RowVersion
            };
    }
}
