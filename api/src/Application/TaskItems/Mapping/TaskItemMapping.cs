using Application.TaskItems.DTOs;
using Domain.Entities;
using Domain.ValueObjects;

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
                RowVersion = entity.RowVersion
            };

        public static TaskItem ToEntity(this TaskItemCreateDto dto)
            => TaskItem.Create(
                dto.ColumnId,
                dto.LaneId,
                dto.ProjectId,
                TaskTitle.Create(dto.Title),
                TaskDescription.Create(dto.Description),
                dto.DueDate,
                dto.SortKey);

        public static TaskItemEditDto ToEditDto(this TaskItemReadDto dto)
            => new()
            {
                Id = dto.Id,
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                RowVersion = dto.RowVersion
            };

        public static TaskItemMoveDto ToMoveDto(this TaskItemReadDto dto)
            => new()
            {
                Id = dto.Id,
                ColumnId = dto.ColumnId,
                LaneId = dto.LaneId,
                SortKey = dto.SortKey,
                RowVersion = dto.RowVersion
            };

        public static TaskItemDeleteDto ToDeleteDto(this TaskItemReadDto dto)
            => new()
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion
            };
    }
}
