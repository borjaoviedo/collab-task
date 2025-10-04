using Application.Users.DTOs;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Users.Mapping
{
    public static class UserMapping
    {
        public static UserReadDto ToReadDto(this User item)
            => new()
            {
                Id = item.Id,
                Email = item.Email.Value,
                Name = item.Name.Value,
                Role = item.Role,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                ProjectMembershipsCount = item.ProjectMemberships?.Count ?? 0
            };

        public static User ToEntity(this UserCreateDto item, byte[] hash, byte[] salt)
            => User.Create(Email.Create(item.Email), UserName.Create(item.Name), hash, salt);

        public static void ApplyRoleChange(this User entity, UserSetRoleDto dto)
        {
            entity.Role = dto.Role;
            entity.RowVersion = dto.RowVersion;
        }
    }
}
