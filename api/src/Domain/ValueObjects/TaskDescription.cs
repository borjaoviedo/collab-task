
namespace Domain.ValueObjects
{
    public sealed class TaskDescription :IEquatable<TaskDescription>
    {
        public string Value { get; }

        private TaskDescription(string value) => Value = value;

        public static TaskDescription Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Task description cannot be empty.", nameof(value));

            value = value.Trim();

            if (value.Length < 2 || value.Length > 2000)
                throw new ArgumentException("Task description must be between 2 and 2000 characters", nameof(value));

            return new TaskDescription(value);
        }

        public override string ToString() => Value;

        public bool Equals(TaskDescription? other) =>
            other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is TaskDescription o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(TaskDescription? a, TaskDescription? b) => Equals(a, b);

        public static bool operator !=(TaskDescription? a, TaskDescription? b) => !Equals(a, b);

        public static implicit operator string(TaskDescription tDescription) => tDescription.Value;
    }
}
