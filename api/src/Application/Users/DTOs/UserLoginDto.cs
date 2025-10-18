namespace Application.Users.DTOs
{
    public sealed class UserLoginDto
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
    }
}
