namespace Api.Auth.DTOs
{
    public sealed class AuthTokenReadDto
    {
        public string AccessToken { get; init; } = default!;
        public string TokenType { get; init; } = "Bearer";
        public DateTime ExpiresAtUtc { get; init; }
        public Guid UserId { get; init; }
        public string Email { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string Role { get; init; } = default!;
    }
}
