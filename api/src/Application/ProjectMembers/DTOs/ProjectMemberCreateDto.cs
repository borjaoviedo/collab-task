using Domain.Enums;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberCreateDto
    {
        public Guid UserId { get; set; }
        public ProjectRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
    }
}
