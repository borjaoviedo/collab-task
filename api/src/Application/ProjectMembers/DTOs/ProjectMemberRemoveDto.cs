
namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberRemoveDto
    {
        public DateTimeOffset? RemovedAt { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
