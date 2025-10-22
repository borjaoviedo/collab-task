using Domain.Enums;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberCreateDto
    {
        public required Guid UserId { get; init; }
        public required ProjectRole Role { get; init; }
    }
}
