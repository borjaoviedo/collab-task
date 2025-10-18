using Domain.Enums;

namespace Application.Users.DTOs
{
    public sealed class UserChangeRoleDto
    {
        public required UserRole NewRole { get; init; }
    }
}
