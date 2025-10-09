using Domain.Enums;

namespace Domain.Entities
{
    public sealed class ProjectMember
    {
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public ProjectRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }
        public DateTimeOffset? RemovedAt { get; set; }
        public byte[] RowVersion { get; set; } = default!;
        public Project Project { get; set; } = null!;
        public User User { get; set; } = null!;

        private ProjectMember() { }

        public static ProjectMember Create(Guid projectId, Guid userId, ProjectRole role, DateTimeOffset joinedAtUtc)
        {
            if (projectId == Guid.Empty) throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));

            return new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role,
                JoinedAt = joinedAtUtc
            };
        }

        public void ChangeRole(ProjectRole newRole) => Role = newRole;
        public void Remove(DateTimeOffset? removedAtUtc) => RemovedAt = removedAtUtc;
    }
}
