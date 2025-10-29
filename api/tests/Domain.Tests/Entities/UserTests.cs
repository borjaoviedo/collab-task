using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using TestHelpers.Common;

namespace Domain.Tests.Entities
{
    public class UserTests
    {
        private static readonly Email _defaultEmail = Email.Create("email@test.com");
        private static readonly UserName _defaultUserName = UserName.Create("username");
        private static readonly byte[] _validHash = TestDataFactory.Bytes(32);
        private static readonly byte[] _validSalt = TestDataFactory.Bytes(16);

        private readonly User _defaultUser = User.Create(
            _defaultEmail,
            _defaultUserName,
            _validHash,
            _validSalt);

        [Fact]
        public void Defaults_RoleIsUser_And_ProjectMemberships_AreInitialized()
        {
            var user = _defaultUser;

            user.Role.Should().Be(UserRole.User);
            user.ProjectMemberships.Should().NotBeNull();
            user.ProjectMemberships.Should().BeEmpty();
        }

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var user = _defaultUser;

            user.Email.Should().Be(_defaultEmail);
            user.Name.Should().Be(_defaultUserName);
            user.PasswordHash.Should().BeSameAs(_validHash);
            user.PasswordSalt.Should().BeSameAs(_validSalt);
        }

        [Fact]
        public void Role_Can_Be_Changed()
        {
            var user = _defaultUser;

            user.Role.Should().Be(UserRole.User);
            user.ChangeRole(UserRole.Admin);
            user.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public void ProjectMemberships_Add_And_Remove_Work()
        {
            var user = _defaultUser;

            var projectMember = ProjectMember.Create(
                projectId: Guid.NewGuid(),
                user.Id,
                ProjectRole.Owner);

            user.ProjectMemberships.Add(projectMember);

            user.ProjectMemberships.Should().HaveCount(1);
            user.ProjectMemberships.Single().Should().BeSameAs(projectMember);

            user.ProjectMemberships.Remove(projectMember);

            user.ProjectMemberships.Should().BeEmpty();
        }

        [Fact]
        public void UpdatedAt_Should_Not_Be_Before_CreatedAt_When_Assigned()
        {
            var user = _defaultUser;

            user.UpdatedAt.Should().BeOnOrAfter(user.CreatedAt);
        }
    }
}
