using Application.Tests.Common.Helpers;

namespace Application.Tests.Common.Fixtures
{
    public abstract class BaseTest
    {
        protected static readonly DateTimeOffset FixedNow = new(2025, 10, 22, 12, 0, 0, TimeSpan.Zero);

        protected readonly FakeClock Clock = new(FixedNow);
    }
}
