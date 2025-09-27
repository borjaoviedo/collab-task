namespace Application.Users.DTOs
{
    public sealed class UserCreateDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
