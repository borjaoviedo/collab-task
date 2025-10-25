using Domain.Common;
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
            Guards.NotEmpty(taskId);
            Guards.NotEmpty(userId);
            Guards.EnumDefined(role);

            return new TaskAssignment
            {
                TaskId = taskId,
                UserId = userId,
                Role = role
            };
        }

        public static TaskAssignment AssignOwner(Guid taskId, Guid userId)
        {
            Guards.NotEmpty(taskId);
            Guards.NotEmpty(userId);

            return Create(taskId, userId, TaskRole.Owner);
        }

        public static TaskAssignment AssignCoOwner(Guid taskId, Guid userId)
        {
            Guards.NotEmpty(taskId);
            Guards.NotEmpty(userId);

            return Create(taskId, userId, TaskRole.CoOwner);
        }

        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion);
            RowVersion = rowVersion;
        }

        internal void SetRole(TaskRole role)
        {
            Guards.EnumDefined(role);
            Role = role;
        }
    }
}
