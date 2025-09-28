
using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class Project : IAuditable
    {
        public Guid Id { get; set; }
        public required ProjectName Name { get; set; }
        public ProjectSlug Slug { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public byte[] RowVersion { get; set; } = default!;
        public ICollection<ProjectMember> Members { get; set; } = [];
    }
}
