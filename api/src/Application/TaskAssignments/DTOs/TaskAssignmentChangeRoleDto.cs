using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentChangeRoleDto
    {
        public required Guid UserId { get; set; }
        public required TaskRole NewRole { get; set; }
    }
}
