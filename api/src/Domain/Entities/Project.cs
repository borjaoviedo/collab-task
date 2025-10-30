using Domain.Common;
using Domain.Common.Abstractions;
using Domain.Common.Exceptions;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a project containing members, lanes, and tasks.
    /// </summary>
    public sealed class Project : IAuditable
    {
        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }
        public ProjectName Name { get; private set; } = default!;
        public ProjectSlug Slug { get; private set; } = default!;
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;
        public ICollection<ProjectMember> Members { get; private set; } = [];

        private Project() { }

        /// <summary>
        /// Creates a new project and assigns the creator as the owner.
        /// </summary>
        public static Project Create(Guid ownerId, ProjectName name)
        {
            Guards.NotEmpty(ownerId);

            var project = new Project()
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = name,
                Slug = ProjectSlug.Create(name)
            };

            project.AddMember(ownerId, ProjectRole.Owner);
            return project;
        }

        /// <summary>Renames the project and regenerates its slug if the name changes.</summary>
        public void Rename(ProjectName newName)
        {
            if (Name.Equals(newName)) return;
            Name = newName;
            Slug = ProjectSlug.Create(newName);
        }

        /// <summary>Adds a new member to the project after validating membership and ownership rules.</summary>
        public void AddMember(Guid userId, ProjectRole role)
        {
            Guards.NotEmpty(userId);
            Guards.EnumDefined(role);

            if (Members.Any(m => m.UserId == userId && m.RemovedAt == null))
                throw new DuplicateEntityException("User already member.");

            if (role == ProjectRole.Owner && Members.Any(m => m.Role == ProjectRole.Owner && m.RemovedAt == null))
                throw new DomainRuleViolationException("Project already has an owner.");

            Members.Add(ProjectMember.Create(Id, userId, role));
        }

        /// <summary>Removes a project member, enforcing ownership constraints.</summary>
        public void RemoveMember(Guid userId, DateTimeOffset removedAtUtc)
        {
            Guards.NotEmpty(userId);

            var member = GetMember(userId);

            if (member.Role == ProjectRole.Owner)
                throw new DomainRuleViolationException("Transfer ownership before removing the owner.");

            member.Remove(removedAtUtc);
        }

        /// <summary>Changes a member’s role while enforcing ownership transfer and demotion rules.</summary>
        public void ChangeMemberRole(Guid userId, ProjectRole newRole)
        {
            Guards.NotEmpty(userId);
            Guards.EnumDefined(newRole);

            var member = GetMember(userId);

            if (newRole == ProjectRole.Owner)
            {
                if (Members.Any(m => m.Role == ProjectRole.Owner && m.RemovedAt == null))
                    throw new DomainRuleViolationException("Project already has an owner.");

                OwnerId = userId;
            }
            else if (member.Role == ProjectRole.Owner && newRole != ProjectRole.Owner)
            {
                throw new DomainRuleViolationException("Cannot demote current owner. Transfer first.");
            }

            member.ChangeRole(newRole);
        }

        /// <summary>Transfers ownership from the current owner to another active member.</summary>
        public void TransferOwnership(Guid newOwnerId)
        {
            Guards.NotEmpty(newOwnerId);
            if (OwnerId == newOwnerId) return;

            var target = Members.FirstOrDefault(m => m.UserId == newOwnerId && m.RemovedAt == null)
                ?? throw new DomainRuleViolationException("New owner must be an active member.");

            var current = Members.First(m => m.Role == ProjectRole.Owner && m.RemovedAt == null);

            current.ChangeRole(ProjectRole.Admin);
            target.ChangeRole(ProjectRole.Owner);
            OwnerId = newOwnerId;
        }

        /// <summary>Sets the concurrency token after persistence.</summary>
        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion);
            RowVersion = rowVersion;
        }

        /// <summary>Gets an active member or throws if not found.</summary>
        private ProjectMember GetMember(Guid userId)
            => Members.FirstOrDefault(m => m.UserId == userId && m.RemovedAt == null)
                ?? throw new EntityNotFoundException("Member not found.");
    }
}
