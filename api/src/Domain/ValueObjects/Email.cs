using Domain.Common;
using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public sealed class Email : IEquatable<Email>
    {
        public string Value { get; }

        private Email(string value) => Value = value;

        private static readonly Regex emailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        public static Email Create(string email)
        {
            Guards.NotNullOrWhiteSpace(email);

            email = email.Trim().ToLowerInvariant();

            Guards.MaxLength(email, 256);

            if (email.Contains(' '))
                throw new ArgumentException("Email cannot contain spaces", nameof(email));

            Guards.Matches(email, emailPattern);

            var parts = email.Split('@');
            if (parts.Length != 2)
                throw new ArgumentException("Invalid email format", nameof(email));

            var local = parts[0];
            var domain = parts[1];

            if (local.Length == 0 || domain.Length == 0)
                throw new ArgumentException("Invalid email format", nameof(email));

            return new Email(email);
        }

        public override string ToString() => Value;

        public bool Equals(Email? other)
            => other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is Email o && Equals(o);

        public override int GetHashCode() => Value.GetHashCode();

        public static bool operator ==(Email? a, Email? b) => Equals(a, b);

        public static bool operator !=(Email? a, Email? b) => !Equals(a, b);


        public static implicit operator string(Email email) => email.Value;
    }
}
