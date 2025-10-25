using Domain.Common;

namespace Domain.ValueObjects
{
    public sealed class ColumnName : IEquatable<ColumnName>
    {
        public string Value { get; }

        private ColumnName(string value) => Value = value;

        public static ColumnName Create(string columnName)
        {
            Guards.NotNullOrWhiteSpace(columnName);
            columnName = columnName.Trim();

            Guards.LengthBetween(columnName, 2, 100);
            Guards.NoConsecutiveSpaces(columnName);

            return new ColumnName(columnName);
        }

        public override string ToString() => Value;

        public bool Equals(ColumnName? other)
            => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is ColumnName o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(ColumnName? a, ColumnName? b) => Equals(a, b);

        public static bool operator !=(ColumnName? a, ColumnName? b) => !Equals(a, b);

        public static implicit operator string(ColumnName cName) => cName.Value;
    }
}
