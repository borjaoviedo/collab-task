using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentChangeRoleDto
    {
        public required Guid UserId { get; init; }
        public required TaskRole NewRole { get; init; }
    }
}
