using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a user assignment within a task.
    /// </summary>
    public sealed class TaskAssignment
    {
        public Guid TaskId { get; private set; }
        public Guid UserId { get; private set; }
        public TaskRole Role { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;

        private TaskAssignment() { }

        /// <summary>Creates a new task assignment with a specified role.</summary>
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

        /// <summary>Creates a new assignment designating the user as task owner.</summary>
        public static TaskAssignment AssignOwner(Guid taskId, Guid userId)
        {
            Guards.NotEmpty(taskId);
            Guards.NotEmpty(userId);

            return Create(taskId, userId, TaskRole.Owner);
        }

        /// <summary>Creates a new assignment designating the user as task co-owner.</summary>
        public static TaskAssignment AssignCoOwner(Guid taskId, Guid userId)
        {
            Guards.NotEmpty(taskId);
            Guards.NotEmpty(userId);

            return Create(taskId, userId, TaskRole.CoOwner);
        }

        public void ChangeRole(TaskRole newRole)
        {
            Guards.EnumDefined(newRole);
            if (newRole == Role) return;

            Role = newRole;
        }
    }
}
