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
            var hash = Bytes(64);
            var salt = Bytes(32);
            var created = DateTimeOffset.UtcNow.AddMinutes(-5);
            var updated = DateTimeOffset.UtcNow;

            var u = new User
            {
                Id = id,
                Email = email,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = created,
                UpdatedAt = updated
            };

            u.Id.Should().Be(id);
            u.Email.Should().Be(email);
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
                PasswordHash = Bytes(32),
                PasswordSalt = Bytes(16),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var member = new ProjectMember
            {
                ProjectId = projId,
                UserId = userId,
                Role = ProjectRole.Owner,
                JoinedAt = DateTimeOffset.UtcNow
            };

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
                PasswordHash = Bytes(32),
                PasswordSalt = Bytes(16),
                CreatedAt = created,
                UpdatedAt = updated
            };

            (u.UpdatedAt >= u.CreatedAt).Should().BeTrue();
        }
    }
}
