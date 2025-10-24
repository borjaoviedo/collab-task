using Application.Columns.DTOs;
using Domain.Entities;

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
    }
}
