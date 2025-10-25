using Domain.Common;
using System.Text.RegularExpressions;

namespace Domain.ValueObjects
{
    public sealed class UserName : IEquatable<UserName>
    {
        public string Value { get; }

        private UserName(string value) => Value = value;

        public static UserName Create(string userName)
        {
            Guards.NotNullOrWhiteSpace(userName);
            userName = userName.Trim();

            Guards.LengthBetween(userName, 2, 100);

            if (Regex.IsMatch(userName, @"[^\p{L}\s]"))
                throw new ArgumentException("User name must contain only letters and spaces", nameof(userName));

            Guards.NoConsecutiveSpaces(userName);

            return new UserName(userName);
        }

        public override string ToString() => Value;

        public bool Equals(UserName? other)
            => other is not null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is UserName o && Equals(o);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(UserName? a, UserName? b)
            => a is null ? b is null : a.Equals(b);

        public static bool operator !=(UserName? a, UserName? b) => !Equals(a, b);


        public static implicit operator string(UserName userName) => userName.Value;
    }
}
