using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentReadDto
    {
        public Guid TaskId { get; init; }
        public Guid UserId { get; init; }
        public TaskRole Role { get; init; }
        public string RowVersion { get; init; } = default!;
    }
}
