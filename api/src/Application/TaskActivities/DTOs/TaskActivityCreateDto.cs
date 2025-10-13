using Domain.Enums;

namespace Application.TaskActivities.DTOs
{
    public sealed class TaskActivityCreateDto
    {
        public TaskActivityType Type { get; set; }
        public required string Payload { get; set; }
    }
}
