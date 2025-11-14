namespace Application.Abstractions.Time
{
    /// <summary>
    /// Provides the canonical UTC clock for the application.
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Gets the current instant in UTC.
        /// </summary>
        DateTimeOffset UtcNow { get; }
    }
}
