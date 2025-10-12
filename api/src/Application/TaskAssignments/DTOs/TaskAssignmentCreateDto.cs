using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentCreateDto
    {
        public Guid UserId { get; set; }
        public TaskRole Role { get; set; }
    }
}
