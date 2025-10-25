using System.Runtime.CompilerServices;

namespace Domain.Common
{
    internal static partial class Guards
    {
        public static void NotEmpty(
            Guid value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value == Guid.Empty)
                throw new ArgumentException($"{paramName} cannot be empty.", paramName);
        }

        public static void NotNull<T>(
            T? value,
            [CallerArgumentExpression("value")] string? paramName = null) where T : class
        {
            if (value is null)
                throw new ArgumentNullException(paramName);
        }

        public static void NonNegative(
            int value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be >= 0.");
        }

        public static void NonNegative(
            decimal value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value < 0m) throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be >= 0.");
        }

        public static void EnumDefined<TEnum>(
            TEnum value,
            [CallerArgumentExpression("value")] string? paramName = null) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(paramName, $"Invalid {typeof(TEnum).Name}.");
        }

        public static void NotInPast
            (DateTimeOffset? value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (value is not null && value < DateTimeOffset.UtcNow)
                throw new ArgumentException($"{paramName} cannot be in the past.", paramName);
        }
    }
}
