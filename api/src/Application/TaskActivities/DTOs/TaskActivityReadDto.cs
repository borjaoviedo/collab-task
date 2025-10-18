using Domain.Enums;

namespace Application.TaskActivities.DTOs
{
    public sealed class TaskActivityReadDto
    {
        public Guid Id { get; init; }
        public Guid TaskId { get; init; }
        public Guid ActorId { get; init; }
        public TaskActivityType Type { get; init; }
        public string Payload { get; init; } = default!;
        public DateTimeOffset CreatedAt { get; init; }
    }
}
