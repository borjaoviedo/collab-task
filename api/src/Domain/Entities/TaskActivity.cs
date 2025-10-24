using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class TaskActivity
    {
        public Guid Id { get; private set; }
        public Guid TaskId { get; private set; }
        public Guid ActorId { get; private set; }
        public TaskActivityType Type { get; private set; }
        public ActivityPayload Payload { get; private set; } = default!;
        public DateTimeOffset CreatedAt { get; private set; }

        private TaskActivity() { }

        public static TaskActivity Create(
            Guid taskId,
            Guid userId,
            TaskActivityType type,
            ActivityPayload payload,
            DateTimeOffset createdAt)
        {
            if (taskId == Guid.Empty) throw new ArgumentException("TaskId cannot be empty.", nameof(taskId));
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
            if (!Enum.IsDefined(typeof(TaskActivityType), type))
                throw new ArgumentOutOfRangeException(nameof(type), "Invalid task activity type.");

            return new TaskActivity
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ActorId = userId,
                Type = type,
                Payload = payload,
                CreatedAt = createdAt
            };
        }
    }
}
