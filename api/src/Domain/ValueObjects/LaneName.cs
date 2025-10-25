using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public sealed class LaneName : IEquatable<LaneName>
    {
        public string Value { get; }

        private LaneName(string value) => Value = value;

        public static LaneName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Lane name cannot be empty", nameof(value));

            value = value.Trim();

            if (value.Length < 2 || value.Length > 100)
                throw new ArgumentException("Lane name must be between 2 and 100 characters", nameof(value));

            if (Regex.IsMatch(value, @"\s{2,}"))
                throw new ArgumentException("Lane name cannot contain consecutive spaces.", nameof(value));

            return new LaneName(value);
        }

        public override string ToString() => Value;

        public bool Equals(LaneName? other) =>
            other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is LaneName o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(LaneName? a, LaneName? b) => Equals(a, b);

        public static bool operator !=(LaneName? a, LaneName? b) => !Equals(a, b);

        public static implicit operator string(LaneName pName) => pName.Value;
    }
}
