using Application.Common.Abstractions.Time;

namespace TestHelpers.Common.Time
{
    public static class TestTime
    {
        public static readonly DateTimeOffset FixedNow = new(2025, 10, 22, 12, 0, 0, TimeSpan.Zero);

        public static IDateTimeProvider FixedClock()
            => new FakeClock(FixedNow);

        public static DateTimeOffset FromFixedMinutes(int minutes)
            => FixedNow.AddMinutes(minutes);

        public static DateTimeOffset FromFixedDays(int days)
            => FixedNow.AddDays(days);
    }
}
