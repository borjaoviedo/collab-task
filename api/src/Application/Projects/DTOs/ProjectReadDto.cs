using Domain.Enums;

namespace Application.Projects.DTOs
{
    public sealed class ProjectReadDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = default!;
        public string Slug { get; init; } = default!;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
        public byte[] RowVersion { get; init; } = default!;
        public int MembersCount { get; init; }
        public ProjectRole CurrentUserRole { get; init; }
    }
}
