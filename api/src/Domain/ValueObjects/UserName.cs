using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public sealed class UserName : IEquatable<UserName>
    {
        public string Value { get; }

        private UserName(string value) => Value = value;

        public static UserName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("User name cannot be empty", nameof(value));

            value = value.Trim();

            if (value.Length < 2 || value.Length > 100)
                throw new ArgumentException("User name must be between 2 and 100 characters", nameof(value));

            if (Regex.IsMatch(value, @"[^\p{L}\s]"))
                throw new ArgumentException("User name must contain only letters and spaces", nameof(value));

            if (Regex.IsMatch(value, @"\s{2,}"))
                throw new ArgumentException("User name cannot contain consecutive spaces.", nameof(value));

            return new UserName(value);
        }

        public override string ToString() => Value;

        public bool Equals(UserName? other) =>
        other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is UserName o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(UserName? a, UserName? b) =>
            a is null ? b is null : a.Equals(b);

        public static bool operator !=(UserName? a, UserName? b) => !Equals(a, b);


        public static implicit operator string(UserName userName) => userName.Value;
    }
}
