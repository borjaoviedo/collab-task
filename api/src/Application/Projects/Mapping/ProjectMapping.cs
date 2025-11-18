using Application.Projects.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Projects.Mapping
{
    public static class ProjectMapping
    {
        public static ProjectReadDto ToReadDto(this Project item, Guid currentUserId)
            => new()
            {
                Id = item.Id,
                Name = item.Name.Value,
                Slug = item.Slug.Value,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                RowVersion = item.RowVersion is { Length: > 0 }
                    ? Convert.ToBase64String(item.RowVersion)
                    : string.Empty,
                MembersCount = item.Members.Count(m => m.RemovedAt == null),
                CurrentUserRole = item.Members
                    .FirstOrDefault(x => x.UserId == currentUserId && x.RemovedAt == null)?.Role
                    ?? ProjectRole.Reader
            };
    }
}
