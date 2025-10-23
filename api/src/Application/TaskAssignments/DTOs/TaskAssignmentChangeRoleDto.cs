using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentChangeRoleDto
    {
        public required TaskRole NewRole { get; init; }
    }
}
