using Domain.Common;

namespace Domain.ValueObjects
{
    public sealed class TaskDescription :IEquatable<TaskDescription>
    {
        public string Value { get; }

        private TaskDescription(string value) => Value = value;

        public static TaskDescription Create(string taskDescription)
        {
            Guards.NotNullOrWhiteSpace(taskDescription);
            taskDescription = taskDescription.Trim();

            Guards.LengthBetween(taskDescription, 2, 2000);
            return new TaskDescription(taskDescription);
        }

        public override string ToString() => Value;

        public bool Equals(TaskDescription? other)
            => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is TaskDescription o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(TaskDescription? a, TaskDescription? b) => Equals(a, b);

        public static bool operator !=(TaskDescription? a, TaskDescription? b) => !Equals(a, b);

        public static implicit operator string(TaskDescription tDescription) => tDescription.Value;
    }
}
