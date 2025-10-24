using Domain.Enums;

namespace Domain.Entities
{
    public sealed class TaskAssignment
    {
        public Guid TaskId { get; private set; }
        public Guid UserId { get; private set; }
        public TaskRole Role { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private TaskAssignment() { }

        public static TaskAssignment Create(Guid taskId, Guid userId, TaskRole role)
        {
            CheckTaskAndUserId(taskId, userId);

            if (!Enum.IsDefined(typeof(TaskRole), role))
                throw new ArgumentOutOfRangeException(nameof(role), "Invalid task role.");

            return new TaskAssignment
            {
                TaskId = taskId,
                UserId = userId,
                Role = role
            };
        }

        public static TaskAssignment AssignOwner(Guid taskId, Guid userId)
        {
            CheckTaskAndUserId(taskId, userId);
            return Create(taskId, userId, TaskRole.Owner);
        }

        public static TaskAssignment AssignCoOwner(Guid taskId, Guid userId)
        {
            CheckTaskAndUserId(taskId, userId);
            return Create(taskId, userId, TaskRole.CoOwner);
        }

        private static void CheckTaskAndUserId(Guid taskId, Guid userId)
        {
            if (taskId == Guid.Empty) throw new ArgumentException("TaskId cannot be empty.", nameof(taskId));
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        internal void SetRowVersion(byte[] value)
            => RowVersion = value ?? throw new ArgumentNullException(nameof(value));

        internal void SetRole(TaskRole role) => Role = role;
    }
}
