using Domain.Common;

namespace Domain.ValueObjects
{
    public sealed class LaneName : IEquatable<LaneName>
    {
        public string Value { get; }

        private LaneName(string value) => Value = value;

        public static LaneName Create(string laneName)
        {
            Guards.NotNullOrWhiteSpace(laneName);
            laneName = laneName.Trim();

            Guards.LengthBetween(laneName, 2, 100);
            Guards.NoConsecutiveSpaces(laneName);

            return new LaneName(laneName);
        }

        public override string ToString() => Value;

        public bool Equals(LaneName? other)
            => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is LaneName o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(LaneName? a, LaneName? b) => Equals(a, b);

        public static bool operator !=(LaneName? a, LaneName? b) => !Equals(a, b);

        public static implicit operator string(LaneName pName) => pName.Value;
    }
}
