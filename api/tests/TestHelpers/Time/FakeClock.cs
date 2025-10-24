using Application.Common.Abstractions.Time;

namespace TestHelpers.Time
{
    public sealed class FakeClock(DateTimeOffset now) : IDateTimeProvider
    {
        private readonly DateTimeOffset _now = now;

        public DateTimeOffset UtcNow => _now;
    }
}
