using Application.Users.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using TestHelpers;

namespace Application.Tests.Users.Mapping
{
    public sealed class UserMappingTests
    {
        private readonly byte[] _validHash = TestDataFactory.Bytes(32);
        private readonly byte[] _validSalt = TestDataFactory.Bytes(16);

        [Fact]
        public void ToReadDto_Maps_All_Fields_And_ProjectMembershipsCount()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var projectRole = ProjectRole.Member;

            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Demo User"), _validHash, _validSalt);
            u.ProjectMemberships.Add(ProjectMember.Create(projectId, userId, projectRole));
            u.ProjectMemberships.Add(ProjectMember.Create(projectId, userId, projectRole));

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
