using Application.Columns.DTOs;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Columns.Mapping
{
    public static class ColumnMapping
    {
        public static ColumnReadDto ToReadDto(this Column entity)
            => new()
            {
                Id = entity.Id,
                LaneId = entity.LaneId,
                ProjectId = entity.ProjectId,
                Name = entity.Name.Value,
                Order = entity.Order,
                RowVersion = entity.RowVersion
            };

        public static Column ToEntity(this ColumnCreateDto dto)
            => Column.Create(dto.ProjectId, dto.LaneId, ColumnName.Create(dto.Name), dto.Order);

        public static ColumnRenameDto ToRenameDto(this ColumnReadDto dto)
            => new()
            {
                Id = dto.Id,
                Name = dto.Name,
                RowVersion = dto.RowVersion
            };

        public static ColumnDeleteDto ToDeleteDto(this ColumnReadDto dto)
            => new()
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion
            };
    }
}
