using Application.Abstractions.Time;

namespace TestHelpers.Common.Time
{
    public sealed class FakeClock(DateTimeOffset now) : IDateTimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public DateTimeOffset UtcNow => _now;
    }
}
