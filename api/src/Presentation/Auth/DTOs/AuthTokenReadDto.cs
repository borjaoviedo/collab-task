namespace Api.Auth.DTOs
{
    public sealed class AuthTokenReadDto
    {
        public string AccessToken { get; set; } = default!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAtUtc { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
