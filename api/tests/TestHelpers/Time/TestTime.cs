
using Application.Common.Abstractions.Time;

namespace TestHelpers.Time
{
    public static class TestTime
    {
        public static readonly DateTimeOffset FixedNow = new(2025, 10, 22, 12, 0, 0, TimeSpan.Zero);

        public static DateTimeOffset OffsetFromFixed(int minutes)
            => FixedNow.AddMinutes(minutes);

        public static IDateTimeProvider FixedClock()
            =>  new FakeClock(FixedNow);
    }
}
