using Domain.Common;

namespace Domain.ValueObjects
{
    /// <summary>
    /// Represents the validated display name of a project.
    /// </summary>
    public sealed class ProjectName : IEquatable<ProjectName>
    {
        public string Value { get; }

        private ProjectName(string value) => Value = value;

        public static ProjectName Create(string projectName)
        {
            Guards.NotNullOrWhiteSpace(projectName);
            projectName = projectName.Trim();

            Guards.MaxLength(projectName, 100);

            return new ProjectName(projectName);
        }

        public override string ToString() => Value;

        public bool Equals(ProjectName? other)
            => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is ProjectName o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(ProjectName? a, ProjectName? b) => Equals(a, b);

        public static bool operator !=(ProjectName? a, ProjectName? b) => !Equals(a, b);

        public static implicit operator string(ProjectName pName) => pName.Value;
    }
}
