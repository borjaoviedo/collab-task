using Application.Common.Abstractions.Time;

namespace Application.Tests.Common.Helpers
{
    public sealed class FakeClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
