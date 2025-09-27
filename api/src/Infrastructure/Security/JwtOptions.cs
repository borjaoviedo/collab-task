
namespace Infrastructure.Security
{
    public sealed record JwtOptions
    {
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string SigningKey { get; init; } = string.Empty;
        public int ExpMinutes { get; init; } = 30;
    }
}
