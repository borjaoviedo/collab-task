using Domain.Enums;

namespace Application.Users.DTOs
{
    public sealed class UserSetRoleDto
    {
        public UserRole Role { get; set; }
        public byte[] RowVersion { get; set; } = default!;
    }
}
