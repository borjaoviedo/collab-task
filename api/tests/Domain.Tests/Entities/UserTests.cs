using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.Tests.Entities
{
    public class UserTests
    {
        private static byte[] Bytes(int n) => Enumerable.Repeat((byte)0xAB, n).ToArray();

        [Fact]
        public void Defaults_RoleIsUser_And_ProjectMemberships_AreInitialized()
        {
            var u = User.Create(Email.Create("user@demo.com"), UserName.Create("Demo User"), Bytes(32), Bytes(16));

            u.Role.Should().Be(UserRole.User);
            u.ProjectMemberships.Should().NotBeNull();
            u.ProjectMemberships.Should().BeEmpty();
        }

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var email = Email.Create("dev@demo.com");
            var name = UserName.Create("Demo Dev");
            var hash = Bytes(32);
            var salt = Bytes(16);

            var u = User.Create(email, name, hash, salt);

            u.Email.Should().Be(email);
            u.Name.Should().Be(name);
            u.PasswordHash.Should().BeSameAs(hash);
            u.PasswordSalt.Should().BeSameAs(salt);
        }

        [Fact]
        public void Role_Can_Be_Changed()
        {
            var u = User.Create(Email.Create("owner@demo.com"), UserName.Create("Demo Owner"), Bytes(32), Bytes(16));

            u.Role.Should().Be(UserRole.User);
            u.Role = UserRole.Admin;
            u.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public void ProjectMemberships_Add_And_Remove_Work()
        {
            var u = User.Create(Email.Create("m@demo.com"), UserName.Create("Demo Member"), Bytes(32), Bytes(16));

            var pm = new ProjectMember(Guid.NewGuid(), u.Id, ProjectRole.Owner, DateTimeOffset.UtcNow);

            u.ProjectMemberships.Add(pm);

            u.ProjectMemberships.Should().HaveCount(1);
            u.ProjectMemberships.Single().Should().BeSameAs(pm);

            u.ProjectMemberships.Remove(pm);
            u.ProjectMemberships.Should().BeEmpty();
        }

        [Fact]
        public void UpdatedAt_Should_Not_Be_Before_CreatedAt_When_Assigned()
        {
            var u = User.Create(Email.Create("t@demo.com"), UserName.Create("Demo Time"), Bytes(32), Bytes(16));

            (u.UpdatedAt >= u.CreatedAt).Should().BeTrue();
        }
    }
}
