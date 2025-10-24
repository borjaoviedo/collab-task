using Domain.Enums;

namespace Domain.Entities
{
    public sealed class ProjectMember
    {
        public Guid ProjectId { get; private set; }
        public Guid UserId { get; private set; }
        public ProjectRole Role { get; private set; }
        public DateTimeOffset JoinedAt { get; private set; }
        public DateTimeOffset? RemovedAt { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;
        public Project Project { get; private set; } = default!;
        public User User { get; private set; } = default!;

        private ProjectMember() { }

        public static ProjectMember Create(Guid projectId, Guid userId, ProjectRole role)
        {
            if (projectId == Guid.Empty) throw new ArgumentException("ProjectId cannot be empty.", nameof(projectId));
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));

            return new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTimeOffset.UtcNow
            };
        }

        public void ChangeRole(ProjectRole newRole) => Role = newRole;

        public void Remove(DateTimeOffset? removedAtUtc) => RemovedAt = removedAtUtc;

        public void Restore() => RemovedAt = null;

        internal void SetRowVersion(byte[] value)
            => RowVersion = value ?? throw new ArgumentNullException(nameof(value));

        internal void SetUser(User user) => User = user;

        internal void SetProject(Project project) => Project = project;
    }
}
