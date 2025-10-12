using Domain.Enums;

namespace Application.TaskActivities.DTOs
{
    public sealed class TaskActivityCreateDto
    {
        public Guid TaskId { get; set; }
        public Guid ActorId { get; set; }
        public TaskActivityType Type { get; set; }
        public required string Payload { get; set; }
    }
}
