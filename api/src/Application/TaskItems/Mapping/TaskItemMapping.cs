using Application.TaskItems.DTOs;
using Domain.Entities;

namespace Application.TaskItems.Mapping
{
    public static class TaskItemMapping
    {
        public static TaskItemReadDto ToReadDto(this TaskItem entity)
            => new()
            {
                Id = entity.Id,
                ColumnId = entity.ColumnId,
                LaneId = entity.LaneId,
                ProjectId = entity.ProjectId,
                Title = entity.Title.Value,
                Description = entity.Description.Value,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                DueDate = entity.DueDate,
                SortKey = entity.SortKey,
                RowVersion = entity.RowVersion is { Length: > 0 }
                    ? Convert.ToBase64String(entity.RowVersion)
                    : string.Empty
            };
    }
}
