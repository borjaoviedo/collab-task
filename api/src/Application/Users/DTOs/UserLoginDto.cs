namespace Application.Users.DTOs
{
    public sealed class UserLoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
