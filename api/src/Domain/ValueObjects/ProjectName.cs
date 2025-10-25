
namespace Domain.ValueObjects
{
    public sealed class ProjectName : IEquatable<ProjectName>
    {
        public string Value { get; }

        private ProjectName(string value) => Value = value;

        public static ProjectName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Project name cannot be empty", nameof(value));

            value = value.Trim();

            if (value.Length > 100)
                throw new ArgumentException("Project name too long", nameof(value));

            return new ProjectName(value);
        }

        public override string ToString() => Value;

        public bool Equals(ProjectName? other) =>
            other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is ProjectName o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(ProjectName? a, ProjectName? b) => Equals(a, b);

        public static bool operator !=(ProjectName? a, ProjectName? b) => !Equals(a, b);

        public static implicit operator string(ProjectName pName) => pName.Value;
    }
}
