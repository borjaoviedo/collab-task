using Domain.Common;
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
            Guards.NotEmpty(projectId, nameof(projectId));
            Guards.NotEmpty(userId, nameof(userId));
            Guards.EnumDefined(role, nameof(role));

            return new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTimeOffset.UtcNow
            };
        }

        public void ChangeRole(ProjectRole newRole)
        {
            Guards.EnumDefined(newRole, nameof(newRole));
            if (Role == newRole) return;

            Role = newRole;
        }

        public void Remove(DateTimeOffset? removedAtUtc)
        {
            if (RemovedAt.HasValue) return;
            RemovedAt = removedAtUtc;
        }

        public void Restore() => RemovedAt = null;

        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion, nameof(rowVersion));
            RowVersion = rowVersion;
        }

        internal void SetUser(User user)
        {
            Guards.NotNull(user, nameof(user));
            User = user;
        }

        internal void SetProject(Project project)
        {
            Guards.NotNull(project, nameof(project));
            Project = project;
        }
    }
}
