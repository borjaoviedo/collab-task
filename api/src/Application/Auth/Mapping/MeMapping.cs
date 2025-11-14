using Application.Auth.DTOs;
using Domain.Entities;

namespace Application.Auth.Mapping
{
    public static class MeMapping
    {
        public static MeReadDto ToMeReadDto(this User user)
            => new()
            {
                Id = user.Id,
                Email = user.Email.Value,
                Name = user.Name.Value,
                Role = user.Role,
                ProjectMembershipsCount = user.ProjectMemberships?.Count ?? 0,
            };
    }
}
