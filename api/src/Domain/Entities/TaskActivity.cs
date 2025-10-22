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

        public static TaskActivity Create(Guid taskId, Guid userId, TaskActivityType type, ActivityPayload payload)
        {
            if (taskId == Guid.Empty) throw new ArgumentException("TaskId cannot be empty.", nameof(taskId));
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));

            return new TaskActivity
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ActorId = userId,
                Type = type,
                Payload = payload
            };
        }
    }
}
