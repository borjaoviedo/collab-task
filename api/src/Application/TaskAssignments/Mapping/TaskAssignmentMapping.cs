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
                RowVersion = entity.RowVersion is { Length: > 0 }
                    ? Convert.ToBase64String(entity.RowVersion)
                    : string.Empty
            };
    }
}
