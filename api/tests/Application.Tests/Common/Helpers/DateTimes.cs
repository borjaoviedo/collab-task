
namespace Application.Tests.Common.Helpers
{
    public static class DateTimes
    {
        public static DateTimeOffset NonUtcInstant() =>
            new DateTimeOffset(2025, 10, 06, 12, 00, 00, TimeSpan.FromHours(2));
    }
}
