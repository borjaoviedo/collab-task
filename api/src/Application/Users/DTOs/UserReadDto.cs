using Domain.Enums;

namespace Application.Users.DTOs
{
    public sealed class UserReadDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public UserRole Role { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int ProjectMembershipsCount { get; set; }
    }
}
