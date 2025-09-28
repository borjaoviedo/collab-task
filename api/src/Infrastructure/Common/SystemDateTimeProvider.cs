using Application.Common.Abstractions.Time;

namespace Infrastructure.Common
{
    public sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
