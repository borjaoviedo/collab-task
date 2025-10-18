using Application.Users.DTOs;
using Domain.Entities;

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
                ProjectMembershipsCount = item.ProjectMemberships?.Count ?? 0,
                RowVersion = item.RowVersion
            };
    }
}
