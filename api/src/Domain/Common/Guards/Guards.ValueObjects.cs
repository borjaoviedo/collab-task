using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Domain.Common
{
    internal static partial class Guards
    {
        public static void NotNullOrWhiteSpace(
            string value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
        }

        public static void MaxLength(
            string value,
            int max,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value.Length > max)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be ≤ {max} chars.");
        }

        public static void LengthBetween(
            string value,
            int min,
            int max,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value.Length < min || value.Length > max)
                throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be {min}–{max} chars.");
        }

        public static void Matches(
            string value,
            Regex pattern,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (!pattern.IsMatch(value))
                throw new ArgumentException($"{paramName} has invalid format.", paramName);
        }

        public static void NoConsecutiveSpaces(
            string value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (Regex.IsMatch(value, @"\s{2,}"))
                throw new ArgumentException($"{paramName} cannot contain consecutive spaces.", paramName);
        }
    }
}
