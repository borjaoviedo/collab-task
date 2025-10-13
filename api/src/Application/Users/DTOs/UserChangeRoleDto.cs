using Domain.Enums;

namespace Application.Users.DTOs
{
    public sealed class UserChangeRoleDto
    {
        public UserRole NewRole { get; set; }
    }
}
