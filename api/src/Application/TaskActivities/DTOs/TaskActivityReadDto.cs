using Domain.Enums;

namespace Application.TaskActivities.DTOs
{
    public sealed class TaskActivityReadDto
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid ActorId { get; set; }
        public TaskActivityType Type { get; set; }
        public string Payload { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
