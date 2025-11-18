using Application.Projects.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using TestHelpers.Common;

namespace Application.Tests.Projects.Mapping
{
    public sealed class ProjectMappingTests
    {
        private readonly byte[] _validHash = TestDataFactory.Bytes(32);
        private readonly byte[] _validSalt = TestDataFactory.Bytes(16);

        [Fact]
        public void ToReadDto_Maps_All_Fields()
        {
            var user = User.Create(
                Email.Create("user@demo.com"),
                UserName.Create("Demo User"),
                _validHash,
                _validSalt);
            var project = Project.Create(user.Id, ProjectName.Create("Project Name"));

            var dto = project.ToReadDto(user.Id);

            Assert.Equal(project.Id, dto.Id);
            Assert.Equal(project.Name.Value, dto.Name);
            Assert.Equal(project.Slug.Value, dto.Slug);
            Assert.Equal(project.CreatedAt, dto.CreatedAt);
            Assert.Equal(project.UpdatedAt, dto.UpdatedAt);
            Assert.Equal(project.Members.Count, dto.MembersCount);
            Assert.Equal(ProjectRole.Owner, dto.CurrentUserRole);
        }

        [Fact]
        public void ToReadDto_CurrentUserRole_Depends_On_User_Role()
        {
            var u1 = User.Create(
                Email.Create("user@demo.com"),
                UserName.Create("Owner User"),
                _validHash,
                _validSalt);
            var project = Project.Create(u1.Id, ProjectName.Create("Project Name"));

            var u2 = User.Create(
                Email.Create("user2@demo.com"),
                UserName.Create("Member User"),
                _validHash,
                _validSalt);
            project.AddMember(u2.Id, ProjectRole.Member);

            var u3 = User.Create(
                Email.Create("user3@demo.com"),
                UserName.Create("Reader User"),
                _validHash,
                _validSalt);
            project.AddMember(u3.Id, ProjectRole.Reader);

            var u4 = User.Create(
                Email.Create("user4@demo.com"),
                UserName.Create("Admin User"),
                _validHash,
                _validSalt);
            project.AddMember(u4.Id, ProjectRole.Admin);

            var dto1 = project.ToReadDto(u1.Id);
            var dto2 = project.ToReadDto(u2.Id);
            var dto3 = project.ToReadDto(u3.Id);
            var dto4 = project.ToReadDto(u4.Id);

            Assert.Equal(ProjectRole.Owner, dto1.CurrentUserRole);
            Assert.Equal(ProjectRole.Member, dto2.CurrentUserRole);
            Assert.Equal(ProjectRole.Reader, dto3.CurrentUserRole);
            Assert.Equal(ProjectRole.Admin, dto4.CurrentUserRole);
        }

        [Fact]
        public void ToReadDto_Ignores_Removed_Members()
        {
            var user = User.Create(
                Email.Create("user@demo.com"),
                UserName.Create("Owner User"),
                _validHash,
                _validSalt);
            var project = Project.Create(user.Id, ProjectName.Create("Project Name"));

            var member = User.Create(
                Email.Create("removed@demo.com"),
                UserName.Create("Removed User"),
                _validHash,
                _validSalt);
            project.AddMember(member.Id, ProjectRole.Member);
            project.Members
                .First(m => m.UserId == member.Id)
                .Remove(removedAtUtc: DateTimeOffset.UtcNow);

            var dto = project.ToReadDto(user.Id);

            Assert.Equal(1, dto.MembersCount);
        }
    }
}
