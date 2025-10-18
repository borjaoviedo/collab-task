using Application.Users.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Tests.Users.Mapping
{
    public sealed class UserMappingTests
    {
        private static byte[] Bytes(int n) => Enumerable.Repeat((byte)0x5A, n).ToArray();

        [Fact]
        public void ToReadDto_Maps_All_Fields_And_ProjectMembershipsCount()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var projectRole = ProjectRole.Member;
            var utcNow = DateTimeOffset.UtcNow;

            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Demo User"), Bytes(32), Bytes(16));
            u.ProjectMemberships.Add(ProjectMember.Create(projectId, userId, projectRole, utcNow));
            u.ProjectMemberships.Add(ProjectMember.Create(projectId, userId, projectRole, utcNow));

            var dto = u.ToReadDto();

            Assert.Equal(u.Id, dto.Id);
            Assert.Equal((string)u.Email, dto.Email);
            Assert.Equal((string)u.Name, dto.Name);
            Assert.Equal(u.Role, dto.Role);
            Assert.Equal(u.CreatedAt, dto.CreatedAt);
            Assert.Equal(u.UpdatedAt, dto.UpdatedAt);
            Assert.Equal(2, dto.ProjectMembershipsCount);
        }
    }
}
