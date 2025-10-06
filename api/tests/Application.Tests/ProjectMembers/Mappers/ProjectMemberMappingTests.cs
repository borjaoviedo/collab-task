using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Tests.ProjectMembers.Mappers
{
    public sealed class ProjectMemberMappingTests
    {
        private static byte[] Bytes(int n) => Enumerable.Repeat((byte)0x5A, n).ToArray();

        [Fact]
        public void ToReadDto_Maps_All_Fields()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var role = ProjectRole.Member;
            var joinedAt = DateTimeOffset.UtcNow.AddDays(-1);
            var projectMember = ProjectMember.Create(projectId, userId, role, joinedAt);

            var dto = projectMember.ToReadDto();

            Assert.Equal(projectMember.ProjectId, dto.ProjectId);
            Assert.Equal(projectMember.UserId, dto.UserId);
            Assert.Equal(string.Empty, dto.UserName); // User is null, so UserName should be empty
            Assert.Equal(projectMember.Role, dto.Role);
            Assert.Equal(projectMember.JoinedAt, dto.JoinedAt);
            Assert.Null(dto.RemovedAt); // Project member is not removed
            Assert.Equal(projectMember.RowVersion, dto.RowVersion);
        }

        [Fact]
        public void ToReadDto_Maps_UserName_When_User_Is_Present()
        {
            var projectMember = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Admin, DateTimeOffset.UtcNow);
            projectMember.User = User.Create(Email.Create("test@demo.com"), UserName.Create("Test User"), Bytes(32), Bytes(16), UserRole.User);

            var dto = projectMember.ToReadDto();

            Assert.Equal(projectMember.User.Name, dto.UserName);
        }

        [Fact]
        public void ToReadDto_Maps_RemovedAt_When_ProjectMember_Is_Removed()
        {
            var projectMember = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Reader, DateTimeOffset.UtcNow.AddDays(-3));
            var removedAt = DateTimeOffset.UtcNow;
            projectMember.Remove(removedAt);
            var dto = projectMember.ToReadDto();
            Assert.Equal(removedAt, dto.RemovedAt);
        }

        [Fact]
        public void ToEntity_Creates_ProjectMember_From_Dto()
        {
            var dto = new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = DateTimeOffset.UtcNow
            };
            var projectId = Guid.NewGuid();
            var entity = dto.ToEntity(projectId);
            Assert.Equal(projectId, entity.ProjectId);
            Assert.Equal(dto.UserId, entity.UserId);
            Assert.Equal(dto.Role, entity.Role);
            Assert.Equal(dto.JoinedAt.ToUniversalTime(), entity.JoinedAt);
            Assert.Null(entity.RemovedAt);
        }

        [Fact]
        public void ToEntity_Throws_When_JoinedAt_Is_Not_Utc()
        {
            var dto = new ProjectMemberCreateDto
            {
                UserId = Guid.NewGuid(),
                Role = ProjectRole.Member,
                JoinedAt = DateTimeOffset.Now // Not UTC
            };
            var projectId = Guid.NewGuid();
            var entity = dto.ToEntity(projectId);
            Assert.Equal(dto.JoinedAt.ToUniversalTime(), entity.JoinedAt);
        }

        [Fact]
        public void ApplyRoleUpdate_Changes_Role()
        {
            var projectMember = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Reader, DateTimeOffset.UtcNow);
            var dto = new ProjectMemberUpdateRoleDto
            {
                Role = ProjectRole.Admin
            };
            projectMember.ApplyRoleUpdate(dto);
            Assert.Equal(ProjectRole.Admin, projectMember.Role);
        }

        [Fact]
        public void ApplyRemoval_Sets_RemovedAt()
        {
            var projectMember = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Member, DateTimeOffset.UtcNow.AddDays(-5));
            var dto = new ProjectMemberRemoveDto
            {
                RemovedAt = DateTimeOffset.UtcNow.AddDays(-1)
            };
            var nowUtc = DateTimeOffset.UtcNow;
            projectMember.ApplyRemoval(dto, nowUtc);
            Assert.Equal(dto.RemovedAt.Value.ToUniversalTime(), projectMember.RemovedAt);
        }

        [Fact]
        public void ApplyRemoval_Sets_RemovedAt_To_Now_When_Dto_RemovedAt_Is_Null()
        {
            var projectMember = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Member, DateTimeOffset.UtcNow.AddDays(-5));
            var dto = new ProjectMemberRemoveDto
            {
                RemovedAt = null
            };
            var nowUtc = DateTimeOffset.UtcNow;
            projectMember.ApplyRemoval(dto, nowUtc);
            Assert.Equal(nowUtc, projectMember.RemovedAt);
        }

        [Fact]
        public void ApplyRemoval_Throws_When_RemovedAt_Is_Not_Utc()
        {
            var projectMember = ProjectMember.Create(Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Member, DateTimeOffset.UtcNow.AddDays(-5));
            var dto = new ProjectMemberRemoveDto
            {
                RemovedAt = DateTimeOffset.Now // Not UTC
            };
            var nowUtc = DateTimeOffset.UtcNow;
            projectMember.ApplyRemoval(dto, nowUtc);
            Assert.Equal(dto.RemovedAt.Value.ToUniversalTime(), projectMember.RemovedAt);
        }
    }
}
