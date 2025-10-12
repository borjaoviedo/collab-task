
namespace Application.TaskItems.DTOs
{
    public sealed class TaskItemDeleteDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
