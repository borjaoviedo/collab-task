using Domain.Common.Abstractions;
using Domain.Common.Exceptions;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
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

        public static Project Create(Guid ownerId, ProjectName name)
        {
            CheckUserId(ownerId);

            var p = new Project()
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = name,
                Slug = ProjectSlug.Create(name)
            };

            p.AddMember(ownerId, ProjectRole.Owner);
            return p;
        }

        public void Rename(ProjectName newName)
        {
            if (Name.Equals(newName)) return;

            Name = newName;
            Slug = ProjectSlug.Create(newName);
        }

        public void AddMember(Guid userId, ProjectRole role)
        {
            CheckUserId(userId);
            CheckRole(role);

            if (Members.Any(m => m.UserId == userId && m.RemovedAt == null))
                throw new DuplicateEntityException("User already member.");

            if (role == ProjectRole.Owner && Members.Any(m => m.Role == ProjectRole.Owner && m.RemovedAt == null))
                throw new DomainRuleViolationException("Project already has an owner.");

            Members.Add(ProjectMember.Create(Id, userId, role));
        }

        public void RemoveMember(Guid userId, DateTimeOffset removedAtUtc)
        {
            CheckUserId(userId);

            var member = Members.FirstOrDefault(m => m.UserId == userId && m.RemovedAt == null)
                ?? throw new EntityNotFoundException("Member not found.");

            if (member.Role == ProjectRole.Owner)
                throw new DomainRuleViolationException("Transfer ownership before removing the owner.");

            member.Remove(removedAtUtc);
        }

        public void ChangeMemberRole(Guid userId, ProjectRole newRole)
        {
            CheckUserId(userId);
            CheckRole(newRole);

            var member = Members.FirstOrDefault(m => m.UserId == userId && m.RemovedAt == null)
                ?? throw new EntityNotFoundException("Member not found.");

            if (newRole == ProjectRole.Owner)
            {
                if (Members.Any(x => x.Role == ProjectRole.Owner && x.RemovedAt == null))
                    throw new DomainRuleViolationException("Project already has an owner.");
                OwnerId = userId;
            }
            else if (member.Role == ProjectRole.Owner && newRole != ProjectRole.Owner)
            {
                throw new DomainRuleViolationException("Cannot demote current owner. Transfer first.");
            }

            member.ChangeRole(newRole);
        }

        public void TransferOwnership(Guid newOwnerId)
        {
            CheckUserId(newOwnerId);
            if (OwnerId == newOwnerId) return;

            var target = Members.FirstOrDefault(m => m.UserId == newOwnerId && m.RemovedAt == null)
                ?? throw new DomainRuleViolationException("New owner must be an active member.");

            var current = Members.First(m => m.Role == ProjectRole.Owner && m.RemovedAt == null);

            current.ChangeRole(ProjectRole.Admin);
            target.ChangeRole(ProjectRole.Owner);
            OwnerId = newOwnerId;
        }

        internal void SetRowVersion(byte[] value)
            => RowVersion = value ?? throw new ArgumentNullException(nameof(value));

        private static void CheckUserId(Guid userId)
        {
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        private static void CheckRole(ProjectRole role)
        {
            if (!Enum.IsDefined(typeof(ProjectRole), role))
                throw new ArgumentOutOfRangeException(nameof(role), "Invalid project role.");
        }

    }
}
