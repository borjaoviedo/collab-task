using Domain.Enums;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberReadDto
    {
        public Guid ProjectId { get; init; }
        public Guid UserId { get; init; }
        public string UserName { get; init; } = default!;
        public string Email { get; init; } = default!;
        public ProjectRole Role { get; init; }
        public DateTimeOffset JoinedAt { get; init; }
        public DateTimeOffset? RemovedAt { get; init; }
        public string RowVersion { get; init; } = default!;
    }
}
