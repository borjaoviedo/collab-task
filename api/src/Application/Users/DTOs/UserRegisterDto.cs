namespace Application.Users.DTOs
{
    public sealed class UserRegisterDto
    {
        public required string Email { get; init; }
        public required string Name { get; init; }
        public required string Password { get; init; }
    }
}
