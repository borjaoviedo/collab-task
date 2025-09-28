
namespace Infrastructure.Security
{
    public sealed record JwtOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SigningKey { get; set; } = string.Empty;
        public int ExpMinutes { get; set; } = 30;
    }
}
