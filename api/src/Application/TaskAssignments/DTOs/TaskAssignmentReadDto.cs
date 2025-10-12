using Domain.Enums;

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentReadDto
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public TaskRole Role { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
