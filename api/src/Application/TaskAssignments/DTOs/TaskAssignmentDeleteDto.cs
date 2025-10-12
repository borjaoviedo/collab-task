

namespace Application.TaskAssignments.DTOs
{
    public sealed class TaskAssignmentDeleteDto
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
