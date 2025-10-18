using Domain.Enums;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberCreateDto
    {
        public required Guid UserId { get; set; }
        public required ProjectRole Role { get; set; }
        public required DateTimeOffset JoinedAt { get; set; }
    }
}
