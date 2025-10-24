using Domain.Common;
using Domain.Common.Abstractions;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
    public sealed class User : IAuditable
    {
        public Guid Id { get; private set; }
        public Email Email { get; private set; } = default!;
        public UserName Name { get; private set; } = default!;
        public byte[] PasswordHash { get; private set; } = default!;
        public byte[] PasswordSalt { get; private set; } = default!;
        public UserRole Role { get; private set; } = UserRole.User;
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }
        public byte[] RowVersion { get; private set; } = default!;
        public ICollection<ProjectMember> ProjectMemberships { get; private set; } = [];

        private User() { }

        public static User Create(
            Email email,
            UserName name,
            byte[] hash,
            byte[] salt,
            UserRole role = UserRole.User)
        {
            Guards.EnumDefined(role, nameof(role));

            return new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = name,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = role
            };
        }

        public void Rename(UserName newName) => Name = newName;

        public void ChangeRole(UserRole newRole)
        {
            Guards.EnumDefined(newRole, nameof(newRole));
            if (Role == newRole) return;

            Role = newRole;
        }

        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion, nameof(rowVersion));
            RowVersion = rowVersion;
        }
    }
}
