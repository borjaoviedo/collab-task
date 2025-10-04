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
            var u = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("user@demo.com"),
                Name = UserName.Create("Demo User"),
                PasswordHash = Bytes(32),
                PasswordSalt = Bytes(16),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            u.Role.Should().Be(UserRole.User);
            u.ProjectMemberships.Should().NotBeNull();
            u.ProjectMemberships.Should().BeEmpty();
        }

        [Fact]
        public void Set_All_Core_Properties_Assigns_Correctly()
        {
            var id = Guid.NewGuid();
            var email = Email.Create("dev@demo.com");
            var name = UserName.Create("Demo Dev");
            var hash = Bytes(64);
            var salt = Bytes(32);
            var created = DateTimeOffset.UtcNow.AddMinutes(-5);
            var updated = DateTimeOffset.UtcNow;

            var u = new User
            {
                Id = id,
                Email = email,
                Name = name,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = created,
                UpdatedAt = updated
            };

            u.Id.Should().Be(id);
            u.Email.Should().Be(email);
            u.Name.Should().Be(name);
            u.PasswordHash.Should().BeSameAs(hash);
            u.PasswordSalt.Should().BeSameAs(salt);
            u.CreatedAt.Should().Be(created);
            u.UpdatedAt.Should().Be(updated);
        }

        [Fact]
        public void Role_Can_Be_Changed()
        {
            var u = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("owner@demo.com"),
                Name = UserName.Create("Demo Owner"),
                PasswordHash = Bytes(32),
                PasswordSalt = Bytes(16),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            u.Role.Should().Be(UserRole.User);
            u.Role = UserRole.Admin;
            u.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public void ProjectMemberships_Add_And_Remove_Work()
        {
            var userId = Guid.NewGuid();
            var projId = Guid.NewGuid();

            var u = new User
            {
                Id = userId,
                Email = Email.Create("m@demo.com"),
                Name = UserName.Create("Demo Member"),
                PasswordHash = Bytes(32),
                PasswordSalt = Bytes(16),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var member = new ProjectMember(projId, userId, ProjectRole.Owner, DateTimeOffset.UtcNow);

            u.ProjectMemberships.Add(member);

            u.ProjectMemberships.Should().HaveCount(1);
            u.ProjectMemberships.Single().Should().BeSameAs(member);

            u.ProjectMemberships.Remove(member);
            u.ProjectMemberships.Should().BeEmpty();
        }

        [Fact]
        public void RowVersion_Assignable()
        {
            var rv = Bytes(8);

            var u = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("v@demo.com"),
                Name = UserName.Create("Demo RowVersion"),
                PasswordHash = Bytes(32),
                PasswordSalt = Bytes(16),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                RowVersion = rv
            };

            u.RowVersion.Should().BeSameAs(rv);
        }

        [Fact]
        public void UpdatedAt_Should_Not_Be_Before_CreatedAt_When_Assigned()
        {
            var created = DateTimeOffset.UtcNow;
            var updated = created.AddMinutes(1);

            var u = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("t@demo.com"),
                Name = UserName.Create("Demo Time"),
                PasswordHash = Bytes(32),
                PasswordSalt = Bytes(16),
                CreatedAt = created,
                UpdatedAt = updated
            };

            (u.UpdatedAt >= u.CreatedAt).Should().BeTrue();
        }
    }
}
