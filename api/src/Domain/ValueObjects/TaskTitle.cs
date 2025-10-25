using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public sealed class TaskTitle : IEquatable<TaskTitle>
    {
        public string Value { get; }

        private TaskTitle(string value) => Value = value;

        public static TaskTitle Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Task title name cannot be empty", nameof(value));

            value = value.Trim();

            if (value.Length < 2 || value.Length > 100)
                throw new ArgumentException("Task title must be between 2 and 100 characters", nameof(value));

            if (Regex.IsMatch(value, @"\s{2,}"))
                throw new ArgumentException("Task title cannot contain consecutive spaces.", nameof(value));

            return new TaskTitle(value);
        }

        public override string ToString() => Value;

        public bool Equals(TaskTitle? other) =>
            other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is TaskTitle o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(TaskTitle? a, TaskTitle? b) => Equals(a, b);

        public static bool operator !=(TaskTitle? a, TaskTitle? b) => !Equals(a, b);

        public static implicit operator string(TaskTitle tTitle) => tTitle.Value;
    }
}
