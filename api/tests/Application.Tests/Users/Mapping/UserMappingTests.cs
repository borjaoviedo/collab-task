using Application.Users.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using TestHelpers.Common;
using TestHelpers.Common.Testing;

namespace Application.Tests.Users.Mapping
{
    [UnitTest]
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

            var entity = User.Create(
                Email.Create("user@demo.com"),
                UserName.Create("Demo User"),
                _validHash,
                _validSalt);
            entity.ProjectMemberships.Add(ProjectMember.Create(projectId, userId, projectRole));
            entity.ProjectMemberships.Add(ProjectMember.Create(projectId, userId, projectRole));

            var dto = entity.ToReadDto();

            Assert.Equal(entity.Id, dto.Id);
            Assert.Equal((string)entity.Email, dto.Email);
            Assert.Equal((string)entity.Name, dto.Name);
            Assert.Equal(entity.Role, dto.Role);
            Assert.Equal(entity.CreatedAt, dto.CreatedAt);
            Assert.Equal(entity.UpdatedAt, dto.UpdatedAt);
            Assert.Equal(2, dto.ProjectMembershipsCount);
        }
    }
}
