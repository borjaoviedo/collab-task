using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentCreateDto
    {
        public required Guid UserId { get; set; }
        public required TaskRole Role { get; set; }
    }
}
