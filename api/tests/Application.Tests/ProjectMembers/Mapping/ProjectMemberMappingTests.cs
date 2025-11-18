using Application.ProjectMembers.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using TestHelpers.Common;
using TestHelpers.Common.Testing;

namespace Application.Tests.ProjectMembers.Mapping
{
    [UnitTest]
    public sealed class ProjectMemberMappingTests
    {
        [Fact]
        public void ToReadDto_Maps_All_Fields()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var role = ProjectRole.Member;
            var projectMember = ProjectMember.Create(projectId, userId, role);

            var dto = projectMember.ToReadDto();

            Assert.Equal(projectMember.ProjectId, dto.ProjectId);
            Assert.Equal(projectMember.UserId, dto.UserId);
            Assert.Equal(string.Empty, dto.UserName); // User is null, so UserName should be empty
            Assert.Equal(string.Empty, dto.Email); // User is null, so Email should be empty
            Assert.Equal(projectMember.Role, dto.Role);
            Assert.Equal(projectMember.JoinedAt, dto.JoinedAt);
            Assert.Null(dto.RemovedAt); // Project member is not removed
        }

        [Fact]
        public void ToReadDto_Maps_UserName_And_Email_When_User_Is_Present()
        {
            var projectMember = ProjectMember.Create(
                projectId: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                ProjectRole.Admin);
            var user = User.Create(
                Email.Create("test@demo.com"),
                UserName.Create("Test User"),
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt(),
                UserRole.User);
            projectMember.SetUser(user);

            var dto = projectMember.ToReadDto();

            Assert.Equal(projectMember.User.Name, dto.UserName);
            Assert.Equal(projectMember.User.Email, dto.Email);
        }

        [Fact]
        public void ToReadDto_Maps_RemovedAt_When_ProjectMember_Is_Removed()
        {
            var projectMember = ProjectMember.Create(
                projectId: Guid.NewGuid(),
                userId: Guid.NewGuid(),
                ProjectRole.Reader);

            var removedAt = DateTimeOffset.UtcNow;
            projectMember.Remove(removedAt);

            var dto = projectMember.ToReadDto();
            Assert.Equal(removedAt, dto.RemovedAt);
        }
    }
}
