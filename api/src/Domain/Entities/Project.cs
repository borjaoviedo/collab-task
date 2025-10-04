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

        public void AddMember(Guid userId, ProjectRole role, DateTimeOffset joinedAtUtc)
        {
            if (Members.Any(m => m.UserId == userId && m.RemovedAt == null))
                throw new DuplicateEntityException("User already member.");

            if (role == ProjectRole.Owner && Members.Any(m => m.Role == ProjectRole.Owner && m.RemovedAt == null))
                throw new DomainRuleViolationException("Project already has an owner.");

            Members.Add(new ProjectMember(Id, userId, role, joinedAtUtc));
        }
    }
}
