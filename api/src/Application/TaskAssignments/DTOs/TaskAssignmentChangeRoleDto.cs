using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentChangeRoleDto
    {
        public Guid UserId { get; set; }
        public TaskRole NewRole { get; set; }
    }
}
