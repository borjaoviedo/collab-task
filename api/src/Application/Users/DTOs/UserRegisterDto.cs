namespace Application.Users.DTOs
{
    public sealed class UserRegisterDto
    {
        public required string Email { get; set; }
        public required string Name { get; set; }
        public required string Password { get; set; }
    }
}
