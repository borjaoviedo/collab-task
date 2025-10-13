using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class TaskActivity
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid ActorId { get; set; }
        public TaskActivityType Type { get; set; }
        public required ActivityPayload Payload { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        private TaskActivity() { }

        public static TaskActivity Create(Guid taskId, Guid actorId, TaskActivityType type, ActivityPayload payload)
        {
            if (taskId == Guid.Empty) throw new ArgumentException("TaskId cannot be empty.", nameof(taskId));
            if (actorId == Guid.Empty) throw new ArgumentException("ActorId cannot be empty.", nameof(actorId));

            return new TaskActivity
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ActorId = actorId,
                Type = type,
                Payload = payload
            };
        }
    }
}
