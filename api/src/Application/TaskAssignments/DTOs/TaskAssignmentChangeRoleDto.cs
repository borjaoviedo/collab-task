using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentChangeRoleDto
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public TaskRole NewRole { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
