using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentCreateDto
    {
        public required Guid UserId { get; init; }
        public required TaskRole Role { get; init; }
    }
}
