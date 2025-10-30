using Domain.Common;
using Domain.Enums;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a user’s membership within a project.
    /// </summary>
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

        /// <summary>Creates a new active project membership.</summary>
        public static ProjectMember Create(Guid projectId, Guid userId, ProjectRole role)
        {
            Guards.NotEmpty(projectId);
            Guards.NotEmpty(userId);
            Guards.EnumDefined(role);

            return new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTimeOffset.UtcNow
            };
        }

        /// <summary>Changes the member’s role if different from the current one.</summary>
        public void ChangeRole(ProjectRole newRole)
        {
            Guards.EnumDefined(newRole);
            if (Role == newRole) return;

            Role = newRole;
        }

        /// <summary>Marks the member as removed if not already removed.</summary>
        public void Remove(DateTimeOffset? removedAtUtc)
        {
            if (RemovedAt.HasValue) return;
            RemovedAt = removedAtUtc;
        }

        /// <summary>Restores a previously removed member.</summary>
        public void Restore() => RemovedAt = null;

        /// <summary>Sets the concurrency token after persistence.</summary>
        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion);
            RowVersion = rowVersion;
        }

        /// <summary>Assigns the user navigation property after materialization.</summary>
        internal void SetUser(User user)
        {
            Guards.NotNull(user);
            User = user;
        }

        /// <summary>Assigns the project navigation property after materialization.</summary>
        internal void SetProject(Project project)
        {
            Guards.NotNull(project);
            Project = project;
        }
    }
}
