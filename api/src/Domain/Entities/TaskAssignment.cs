using Domain.Enums;

namespace Domain.Entities
{
    public sealed class TaskAssignment
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }
        public TaskRole Role { get; set; }
        public byte[] RowVersion { get; set; } = default!;

        private TaskAssignment() { }

        public static TaskAssignment Create(Guid taskId, Guid userId, TaskRole role)
        {
            if (taskId == Guid.Empty) throw new ArgumentException("TaskId cannot be empty.", nameof(taskId));
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));

            return new TaskAssignment
            {
                TaskId = taskId,
                UserId = userId,
                Role = role
            };
        }

        public static TaskAssignment AssignOwner(Guid taskId, Guid userId)
            => Create(taskId, userId, TaskRole.Owner);

        public static TaskAssignment AssignCoOwner(Guid taskId, Guid userId)
            => Create(taskId, userId, TaskRole.CoOwner);
    }
}
