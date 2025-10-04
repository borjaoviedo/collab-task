using Domain.Common.Abstractions;
using Domain.Enums;
using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public sealed class User : IAuditable
    {
        public Guid Id { get; set; }
        public required Email Email { get; set; }
        public required UserName Name { get; set; }
        public byte[] PasswordHash { get; set; } = default!;
        public byte[] PasswordSalt { get; set; } = default!;
        public UserRole Role { get; set; } = UserRole.User;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        [Timestamp] public byte[] RowVersion { get; set; } = default!;
        public ICollection<ProjectMember> ProjectMemberships { get; private set; } = [];
    }
}
