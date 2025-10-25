using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public sealed class Email : IEquatable<Email>
    {
        public string Value { get; }

        private Email(string value) => Value = value;

        public static Email Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty", nameof(value));

            value = value.Trim().ToLowerInvariant();

            if (value.Length > 256)
                throw new ArgumentException("Email too long", nameof(value));

            if (value.Contains(' '))
                throw new ArgumentException("Email cannot contain spaces", nameof(value));

            if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Invalid email format", nameof(value));

            var parts = value.Split('@');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid email format", nameof(value));

            var local = parts[0];
            var domain = parts[1];

            if (local.Length == 0 || domain.Length == 0)
                throw new ArgumentException("Invalid email format", nameof(value));

            return new Email(value);
        }

        public override string ToString() => Value;

        public bool Equals(Email? other) =>
        other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is Email o && Equals(o);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(Email? a, Email? b) => Equals(a, b);

        public static bool operator !=(Email? a, Email? b) => !Equals(a, b);


        public static implicit operator string(Email email) => email.Value;
    }
}
