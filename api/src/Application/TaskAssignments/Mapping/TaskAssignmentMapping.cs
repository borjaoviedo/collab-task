using Application.TaskAssignments.DTOs;
using Domain.Entities;

namespace Application.TaskAssignments.Mapping
{
    public static class TaskAssignmentMapping
    {
        public static TaskAssignmentReadDto ToReadDto(this TaskAssignment entity)
            => new()
            {
                TaskId = entity.TaskId,
                UserId = entity.UserId,
                Role = entity.Role,
                RowVersion = entity.RowVersion
            };

        public static TaskAssignment ToEntity(this TaskAssignmentCreateDto dto)
            => TaskAssignment.Create(dto.TaskId, dto.UserId, dto.Role);

        public static TaskAssignmentChangeRoleDto ToChangeRoleDto(this TaskAssignmentReadDto dto)
            => new()
            {
                TaskId = dto.TaskId,
                UserId = dto.UserId,
                NewRole = dto.Role,
                RowVersion = dto.RowVersion
            };

        public static TaskAssignmentDeleteDto ToDeleteDto(this TaskAssignmentReadDto dto)
            => new()
            {
                TaskId = dto.TaskId,
                UserId = dto.UserId,
                RowVersion = dto.RowVersion
            };
    }
}
