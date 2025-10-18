using Domain.Enums;

namespace Api.Auth.DTOs
{
    public sealed class MeReadDto
    {
        public Guid Id { get; init; }
        public string Email { get; init; } = default!;
        public string Name { get; init; } = default!;
        public UserRole Role { get; init; }
        public int ProjectMembershipsCount { get; init; }
    }
}
