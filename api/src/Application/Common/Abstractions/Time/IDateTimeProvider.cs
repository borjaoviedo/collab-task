namespace Application.Common.Abstractions.Time
{
    public interface IDateTimeProvider
    {
        DateTimeOffset UtcNow { get; }
    }
}
