using Domain.Common.Abstractions;
using Domain.Common.Exceptions;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class Project : IAuditable
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public required ProjectName Name { get; set; }
        public ProjectSlug Slug { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = default!;
        public ICollection<ProjectMember> Members { get; set; } = [];

        private Project() { }

        public static Project Create(Guid ownerId, ProjectName name, DateTimeOffset nowUtc)
        {
            var p = new Project()
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = name,
                Slug = ProjectSlug.Create(name)
            };

            p.AddMember(ownerId, ProjectRole.Owner, nowUtc);
            return p;
        }

        public void Rename(ProjectName newName)
        {
            Name = newName;
            Slug = ProjectSlug.Create(newName);
        }

        public void AddMember(Guid userId, ProjectRole role, DateTimeOffset joinedAtUtc)
        {
            if (Members.Any(m => m.UserId == userId && m.RemovedAt == null))
                throw new DuplicateEntityException("User already member.");

            if (role == ProjectRole.Owner && Members.Any(m => m.Role == ProjectRole.Owner && m.RemovedAt == null))
                throw new DomainRuleViolationException("Project already has an owner.");

            Members.Add(new ProjectMember(Id, userId, role, joinedAtUtc));
        }

        public void RemoveMember(Guid userId, DateTimeOffset removedAtUtc)
        {
            var m = Members.FirstOrDefault(x => x.UserId == userId && x.RemovedAt == null)
                ?? throw new EntityNotFoundException("Member not found.");

            if (m.Role == ProjectRole.Owner)
                throw new DomainRuleViolationException("Transfer ownership before removing the owner.");

            m.Remove(removedAtUtc);
        }

        public void ChangeMemberRole(Guid userId, ProjectRole newRole)
        {
            var m = Members.FirstOrDefault(x => x.UserId == userId && x.RemovedAt == null)
                ?? throw new EntityNotFoundException("Member not found.");

            if (newRole == ProjectRole.Owner)
            {
                if (Members.Any(x => x.Role == ProjectRole.Owner && x.RemovedAt == null))
                    throw new DomainRuleViolationException("Project already has an owner.");
                OwnerId = userId;
            }
            else if (m.Role == ProjectRole.Owner && newRole != ProjectRole.Owner)
            {
                throw new DomainRuleViolationException("Cannot demote current owner. Transfer first.");
            }

            m.Role = newRole;
        }

        public void TransferOwnership(Guid newOwnerId)
        {
            if (OwnerId == newOwnerId) return;

            var target = Members.FirstOrDefault(x => x.UserId == newOwnerId && x.RemovedAt == null)
                ?? throw new DomainRuleViolationException("New owner must be an active member.");

            var current = Members.First(x => x.Role == ProjectRole.Owner && x.RemovedAt == null);

            current.Role = ProjectRole.Admin;
            target.Role = ProjectRole.Owner;
            OwnerId = newOwnerId;
        }
    }
}
