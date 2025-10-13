using Application.Lanes.DTOs;
using Domain.Entities;

namespace Application.Lanes.Mapping
{
    public static class LaneMapping
    {
        public static LaneReadDto ToReadDto(this Lane entity)
            => new()
            {
                Id = entity.Id,
                ProjectId = entity.ProjectId,
                Name = entity.Name.Value,
                Order = entity.Order,
                RowVersion = entity.RowVersion
            };
    }
}
