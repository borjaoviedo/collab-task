using Domain.Common;
using Domain.Common.Abstractions;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a registered user within the system.
    /// </summary>
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

        /// <summary>
        /// Creates a new user with the specified credentials and role.
        /// </summary>
        public static User Create(
            Email email,
            UserName name,
            byte[] hash,
            byte[] salt,
            UserRole role = UserRole.User)
        {
            Guards.EnumDefined(role);

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

        /// <summary>
        /// Renames the user by setting a new name.
        /// </summary>
        public void Rename(UserName newName) => Name = newName;

        /// <summary>
        /// Changes the user's role if the new role differs from the current one.
        /// </summary>
        public void ChangeRole(UserRole newRole)
        {
            Guards.EnumDefined(newRole);
            if (Role == newRole) return;

            Role = newRole;
        }

        /// <summary>
        /// Sets the concurrency token after the entity has been persisted.
        /// </summary>
        internal void SetRowVersion(byte[] rowVersion)
        {
            Guards.NotNull(rowVersion);
            RowVersion = rowVersion;
        }
    }
}
