namespace Api.Auth
{
    public sealed class AuthTokenReadDto
    {
        public string AccessToken { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAtUtc { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
