using Application.Projects.DTOs;
using Application.Projects.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Tests.Projects.Mappers
{
    public sealed class ProjectMappingTests
    {
        private static byte[] Bytes(int n) => Enumerable.Repeat((byte)0x5A, n).ToArray();

        [Fact]
        public void ToReadDto_Maps_All_Fields()
        {
            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Demo User"), Bytes(32), Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);

            var dto = p.ToReadDto(u.Id);

            Assert.Equal(p.Id, dto.Id);
            Assert.Equal(p.Name.Value, dto.Name);
            Assert.Equal(p.Slug.Value, dto.Slug);
            Assert.Equal(p.CreatedAt, dto.CreatedAt);
            Assert.Equal(p.UpdatedAt, dto.UpdatedAt);
            Assert.Equal(p.RowVersion, dto.RowVersion);
            Assert.Equal(p.Members.Count, dto.MembersCount);
            Assert.Equal(ProjectRole.Owner, dto.CurrentUserRole);
        }

        [Fact]
        public void ToReadDto_CurrentUserRole_Depends_On_User_Role()
        {
            var u1 = User.Create(Email.Create("user@demo.com"), UserName.Create("Owner User"), Bytes(32), Bytes(16));
            var p = Project.Create(u1.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);

            var u2 = User.Create(Email.Create("user2@demo.com"), UserName.Create("Member User"), Bytes(32), Bytes(16));
            p.AddMember(u2.Id, ProjectRole.Member, DateTimeOffset.UtcNow);

            var u3 = User.Create(Email.Create("user3@demo.com"), UserName.Create("Reader User"), Bytes(32), Bytes(16));
            p.AddMember(u3.Id, ProjectRole.Reader, DateTimeOffset.UtcNow);

            var u4 = User.Create(Email.Create("user4@demo.com"), UserName.Create("Admin User"), Bytes(32), Bytes(16));
            p.AddMember(u4.Id, ProjectRole.Admin, DateTimeOffset.UtcNow);

            var dto1 = p.ToReadDto(u1.Id);
            var dto2 = p.ToReadDto(u2.Id);
            var dto3 = p.ToReadDto(u3.Id);
            var dto4 = p.ToReadDto(u4.Id);
            Assert.Equal(ProjectRole.Owner, dto1.CurrentUserRole);
            Assert.Equal(ProjectRole.Member, dto2.CurrentUserRole);
            Assert.Equal(ProjectRole.Reader, dto3.CurrentUserRole);
            Assert.Equal(ProjectRole.Admin, dto4.CurrentUserRole);
        }

        [Fact]
        public void ToReadDto_Ignores_Removed_Members()
        {
            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Owner User"), Bytes(32), Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);

            var m = User.Create(Email.Create("removed@demo.com"), UserName.Create("Removed User"), Bytes(32), Bytes(16));
            p.AddMember(m.Id, ProjectRole.Member, DateTimeOffset.UtcNow);
            p.Members.First(x => x.UserId == m.Id).Remove(DateTimeOffset.UtcNow);

            var dto = p.ToReadDto(u.Id);

            Assert.Equal(1, dto.MembersCount);
        }

        [Fact]
        public void ToEntity_FromCreateDto_Sets_All_Properties()
        {
            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Owner User"), Bytes(32), Bytes(16));
            var now = DateTimeOffset.UtcNow;

            var dto = new ProjectCreateDto { Name = "Project Name" };
            var entity = dto.ToEntity(u.Id, now);

            Assert.NotEqual(entity.Id, Guid.Empty);
            Assert.Equal(entity.OwnerId, u.Id);
            Assert.Equal(entity.Name, ProjectName.Create(dto.Name));
            Assert.Equal(entity.Slug, ProjectSlug.Create(dto.Name));
            Assert.Single(entity.Members);
        }

        [Fact]
        public void ToListDto_Maps_All_Fields()
        {
            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Demo User"), Bytes(32), Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);

            var dto = p.ToListDto(u.Id);

            Assert.Equal(p.Id, dto.Id);
            Assert.Equal(p.Name.Value, dto.Name);
            Assert.Equal(p.Slug.Value, dto.Slug);
            Assert.Equal(p.UpdatedAt, dto.UpdatedAt);
            Assert.Equal(p.Members.Count, dto.MembersCount);
            Assert.Equal(ProjectRole.Owner, dto.CurrentUserRole);
        }

        [Fact]
        public void ToListDto_CurrentUserRole_Depends_On_User_Role()
        {
            var u1 = User.Create(Email.Create("user@demo.com"), UserName.Create("Owner User"), Bytes(32), Bytes(16));
            var p = Project.Create(u1.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);

            var u2 = User.Create(Email.Create("user2@demo.com"), UserName.Create("Member User"), Bytes(32), Bytes(16));
            p.AddMember(u2.Id, ProjectRole.Member, DateTimeOffset.UtcNow);

            var u3 = User.Create(Email.Create("user3@demo.com"), UserName.Create("Reader User"), Bytes(32), Bytes(16));
            p.AddMember(u3.Id, ProjectRole.Reader, DateTimeOffset.UtcNow);

            var u4 = User.Create(Email.Create("user4@demo.com"), UserName.Create("Admin User"), Bytes(32), Bytes(16));
            p.AddMember(u4.Id, ProjectRole.Admin, DateTimeOffset.UtcNow);

            var dto1 = p.ToListDto(u1.Id);
            var dto2 = p.ToListDto(u2.Id);
            var dto3 = p.ToListDto(u3.Id);
            var dto4 = p.ToListDto(u4.Id);
            Assert.Equal(ProjectRole.Owner, dto1.CurrentUserRole);
            Assert.Equal(ProjectRole.Member, dto2.CurrentUserRole);
            Assert.Equal(ProjectRole.Reader, dto3.CurrentUserRole);
            Assert.Equal(ProjectRole.Admin, dto4.CurrentUserRole);
        }

        [Fact]
        public void ToListDto_Ignores_Removed_Members()
        {
            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Owner User"), Bytes(32), Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);

            var m = User.Create(Email.Create("removed@demo.com"), UserName.Create("Removed User"), Bytes(32), Bytes(16));
            p.AddMember(m.Id, ProjectRole.Member, DateTimeOffset.UtcNow);
            p.Members.First(x => x.UserId == m.Id).Remove(DateTimeOffset.UtcNow);

            var dto = p.ToListDto(u.Id);

            Assert.Equal(1, dto.MembersCount);
        }

        [Fact]
        public void ToUpdateDto_Maps_All_Fields()
        {
            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Demo User"), Bytes(32), Bytes(16));
            var p = Project.Create(u.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);

            var dto = p.ToUpdateDto();

            Assert.Equal(p.Name.Value, dto.Name);
            Assert.Equal(p.RowVersion, dto.RowVersion);
        }
    }
}
