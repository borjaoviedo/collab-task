using Application.Projects.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

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
                RowVersion = item.RowVersion,
                MembersCount = item.Members.Count(m => m.RemovedAt == null),
                CurrentUserRole = item.Members
                    .FirstOrDefault(x => x.UserId == currentUserId && x.RemovedAt == null)?.Role
                    ?? ProjectRole.Reader
            };

        public static ProjectUpdateDto ToUpdateDto(this Project item)
            => new()
            {
                Name = item.Name.Value,
                RowVersion = item.RowVersion
            };

        public static Project ToEntity(this ProjectCreateDto item, Guid ownerId, DateTimeOffset nowUtc)
            => Project.Create(ownerId, ProjectName.Create(item.Name), nowUtc);

    }
}
