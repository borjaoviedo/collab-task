using Domain.Common;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Represents the validated title of a task.
    /// </summary>
    public sealed class TaskTitle : IEquatable<TaskTitle>
    {
        public string Value { get; }

        private TaskTitle(string value) => Value = value;

        public static TaskTitle Create(string taskTitle)
        {
            Guards.NotNullOrWhiteSpace(taskTitle);
            taskTitle = taskTitle.Trim();

            Guards.LengthBetween(taskTitle, 2, 100);
            Guards.NoConsecutiveSpaces(taskTitle);

            return new TaskTitle(taskTitle);
        }

        public override string ToString() => Value;

        public bool Equals(TaskTitle? other)
            => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is TaskTitle o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(TaskTitle? a, TaskTitle? b) => Equals(a, b);

        public static bool operator !=(TaskTitle? a, TaskTitle? b) => !Equals(a, b);

        public static implicit operator string(TaskTitle tTitle) => tTitle.Value;
    }
}
