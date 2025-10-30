using Application.Common.Abstractions.Time;

namespace Infrastructure.Common.Time
{
    /// <summary>
    /// Provides the current system UTC time for use across the application.
    /// </summary>
    public sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        /// <summary>
        /// Gets the current UTC date and time from the system clock.
        /// </summary>
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
