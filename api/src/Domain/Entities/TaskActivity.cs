using Domain.Common;
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
            Guards.NotEmpty(taskId, nameof(taskId));
            Guards.NotEmpty(userId, nameof(userId));
            Guards.EnumDefined(type, nameof(type));

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
