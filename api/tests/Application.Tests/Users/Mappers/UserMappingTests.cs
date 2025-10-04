using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Tests.Users.Mappers
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

            var u = new User
            {
                Id = userId,
                Email = Email.Create("user@demo.com"),
                Name = UserName.Create("Demo User"),
                Role = UserRole.User,
                CreatedAt = utcNow.AddDays(-1),
                UpdatedAt = utcNow,
            };
            u.ProjectMemberships.Add(new ProjectMember(projectId, userId, projectRole, utcNow));
            u.ProjectMemberships.Add(new ProjectMember(projectId, userId, projectRole, utcNow));

            var dto = u.ToReadDto();

            Assert.Equal(u.Id, dto.Id);
            Assert.Equal((string)u.Email, dto.Email);
            Assert.Equal((string)u.Name, dto.Name);
            Assert.Equal(u.Role, dto.Role);
            Assert.Equal(u.CreatedAt, dto.CreatedAt);
            Assert.Equal(u.UpdatedAt, dto.UpdatedAt);
            Assert.Equal(2, dto.ProjectMembershipsCount);
        }

        [Fact]
        public void ToEntity_FromCreateDto_Sets_Email_Name_Role_User_And_Hash_Salt()
        {
            var create = new UserCreateDto { Email = "user@demo.com", Name = "User Name", Password = "GoodPwd1!" };
            var hash = Bytes(32);
            var salt = Bytes(16);

            var entity = create.ToEntity(hash, salt);

            Assert.Equal(Email.Create("user@demo.com"), entity.Email);
            Assert.Equal(UserName.Create("User Name"), entity.Name);
            Assert.Equal(UserRole.User, entity.Role);
            Assert.Same(hash, entity.PasswordHash);
            Assert.Same(salt, entity.PasswordSalt);
        }

        [Fact]
        public void ApplyRoleChange_Updates_Role_And_RowVersion()
        {
            var entity = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("user@demo.com"),
                Name = UserName.Create("Demo User"),
                Role = UserRole.User,
                RowVersion = Bytes(8)
            };
            var newRv = Bytes(8);
            var dto = new UserSetRoleDto { Role = UserRole.Admin, RowVersion = newRv };

            entity.ApplyRoleChange(dto);

            Assert.Equal(UserRole.Admin, entity.Role);
            Assert.Same(newRv, entity.RowVersion);
        }
    }
}
